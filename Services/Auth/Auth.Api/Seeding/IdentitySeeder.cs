using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Seeding;

/// <summary>
/// Застосовує міграції, створює ролі User/Admin і (поза Production) тестових користувачів.
/// Ідемпотентний: повторний запуск нічого не дублює.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment,
        ILogger logger, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (await roles.RoleExistsAsync(role))
                continue;

            await roles.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Role {Role} created", role);
        }

        if (environment.IsProduction() || !configuration.GetValue("Seed:Enabled", true))
            return;

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(users, logger, configuration["Seed:AdminEmail"], configuration["Seed:AdminPassword"],
            "Platform", "Admin", customerId: null, [AppRoles.User, AppRoles.Admin]);

        await EnsureUserAsync(users, logger, configuration["Seed:UserEmail"], configuration["Seed:UserPassword"],
            "Olena", "Koval", customerId: 1, [AppRoles.User]);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, ILogger logger, string? email,
        string? password, string firstName, string lastName, long? customerId, IReadOnlyList<string> roles)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                CustomerId = customerId
            };

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogWarning("Seed user {Email} was not created: {Errors}", email,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Seed user {UserId} created", user.Id);
        }

        foreach (var role in roles)
        {
            if (!await users.IsInRoleAsync(user, role))
                await users.AddToRoleAsync(user, role);
        }
    }
}
