using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.IdentityServer.Data;
using Platform.IdentityServer.Models;
using Platform.ServiceDefaults.Correlation;

namespace Platform.IdentityServer;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        builder.Services.AddRazorPages();

        // Окрема база IdentityServerDb (окремий SQL Server identityserver-sql в AppHost):
        // ApplicationDbContext (ASP.NET Identity), ConfigurationDbContext і PersistedGrantDbContext.
        var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
            ?? throw new InvalidOperationException("Connection string 'IdentityServerDb' is missing");
        var migrationsAssembly = typeof(HostingExtensions).Assembly.GetName().Name;

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
            // Clients, IdentityResources, ApiResources, ApiScopes → ConfigurationDbContext
            .AddConfigurationStore(options =>
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)))
            // Authorization codes, refresh tokens, consents, ключі підпису → PersistedGrantDbContext
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = db =>
                    db.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                options.EnableTokenCleanup = true;
                options.TokenCleanupInterval = 3600;
            })
            // Користувачі IdentityServer — це ASP.NET Identity (AspNetUsers у тій самій базі)
            .AddAspNetIdentity<ApplicationUser>()
            .AddLicenseSummary();

        builder.Services.AddDataProtection().SetApplicationName("Platform.IdentityServer");

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapDefaultEndpoints();

        // Quickstart UI (Duende): Login, Logout, Consent, Grants, Diagnostics
        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}
