using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Data;
using Platform.IdentityServer.Models;

namespace Platform.IdentityServer;

/// <summary>
/// Застосовує міграції трьох контекстів і заповнює IdentityServerDb: clients, resources, scopes і тестових користувачів.
/// Ідемпотентний: додає лише те, чого ще немає.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeedDataAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedData));

        await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        var configuration = services.GetRequiredService<ConfigurationDbContext>();
        await configuration.Database.MigrateAsync();

        await SeedConfigurationAsync(configuration, app.Configuration, logger);

        if (!app.Environment.IsProduction())
            await SeedUsersAsync(services.GetRequiredService<UserManager<ApplicationUser>>(), logger);
    }

    private static async Task SeedConfigurationAsync(ConfigurationDbContext db, IConfiguration configuration, ILogger logger)
    {
        foreach (var client in Config.Clients(configuration))
        {
            if (await db.Clients.AnyAsync(c => c.ClientId == client.ClientId))
                continue;

            db.Clients.Add(client.ToEntity());
            logger.LogInformation("IdentityServer client {ClientId} added", client.ClientId);
        }

        foreach (var resource in Config.IdentityResources)
        {
            if (!await db.IdentityResources.AnyAsync(r => r.Name == resource.Name))
                db.IdentityResources.Add(resource.ToEntity());
        }

        foreach (var scope in Config.ApiScopes)
        {
            if (!await db.ApiScopes.AnyAsync(s => s.Name == scope.Name))
                db.ApiScopes.Add(scope.ToEntity());
        }

        foreach (var resource in Config.ApiResources)
        {
            if (!await db.ApiResources.AnyAsync(r => r.Name == resource.Name))
                db.ApiResources.Add(resource.ToEntity());
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> users, ILogger logger)
    {
        await EnsureUserAsync(users, logger, "alice", "alice@cosmetics.local", "Alice", "Smith");
        await EnsureUserAsync(users, logger, "bob", "bob@cosmetics.local", "Bob", "Brown");
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, ILogger logger, string userName,
        string email, string givenName, string familyName)
    {
        if (await users.FindByNameAsync(userName) is not null)
            return;

        var user = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, "Pass123$");
        if (!result.Succeeded)
        {
            logger.LogWarning("Seed user {UserName} was not created: {Errors}", userName,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddClaimsAsync(user,
        [
            new Claim(JwtClaimTypes.Name, $"{givenName} {familyName}"),
            new Claim(JwtClaimTypes.GivenName, givenName),
            new Claim(JwtClaimTypes.FamilyName, familyName),
            new Claim(JwtClaimTypes.Email, email)
        ]);

        logger.LogInformation("Seed user {UserName} created", userName);
    }
}
