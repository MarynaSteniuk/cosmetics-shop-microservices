using AutoMapper;
using Catalog.Api.Authorization;
using Catalog.Api.ErrorHandling;
using Catalog.Api.Grpc;
using Catalog.Api.HealthChecks;
using Catalog.Api.IntegrationEvents;
using Catalog.Api.Metrics;
using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validators;
using Catalog.Dal.Interfaces;
using Catalog.Dal.Seeding;
using Catalog.Dal;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Platform.ServiceDefaults.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---------- Лаб. 09: автентифікація — токени трьох видавців ----------
var authServiceIssuer = PlatformAuthentication.AuthServiceIssuer(builder.Configuration);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = PlatformAuthentication.MultiIssuerScheme;
        options.DefaultChallengeScheme = PlatformAuthentication.MultiIssuerScheme;
    })
    .AddPolicyScheme(PlatformAuthentication.MultiIssuerScheme, "Auth Service, IdentityServer або Keycloak", options =>
        options.ForwardDefaultSelector = context => PlatformAuthentication.SelectScheme(context, authServiceIssuer))
    // Частина 1: JWT від Auth Service (HMAC-SHA256, Jwt:SecretKey з AppHost)
    .AddJwtBearer(PlatformAuthentication.AuthServiceScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = PlatformAuthentication.AuthServiceValidation(builder.Configuration);
    })
    // Частина 2: Duende IdentityServer — Authority + Audience (ApiResource)
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = PlatformAuthentication.IdentityServerAuthority(builder.Configuration);
        options.Audience = "products_api";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.AuthenticationType = PlatformAuthentication.IdentityServerScheme;
        options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
    })
    // Частина 3: Keycloak (Aspire.Keycloak.Authentication), realm DevRealmDrivers, resource server products-api
    .AddKeycloakJwtBearer(PlatformAuthentication.KeycloakServiceName, PlatformAuthentication.KeycloakRealm,
        PlatformAuthentication.KeycloakScheme, options =>
        {
            options.Audience = "products-api";
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.AuthenticationType = PlatformAuthentication.KeycloakScheme;
            options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
            options.Events = PlatformAuthentication.KeycloakEvents();
        });

// ---------- Лаб. 09: авторизація — політики за дозволами матриці (Keycloak permissions) ----------
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission)));

    // Частина 2: політика за scope IdentityServer для machine-to-machine викликів (Aggregator, Client Credentials)
    options.AddPolicy("products.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(PlatformAuthentication.ScopeClaimType, "products.read"));
});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());
builder.Services.AddGrpcHealthChecks();

builder.Services.AddCatalogHealthChecks(builder.Configuration);
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

builder.Services.AddCatalogMetrics();

var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing");

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.AddRabbitMQClient("messaging");
builder.Services.AddEventPublisher();
builder.Services.AddCatalogIntegrationEvents();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<CatalogMappingProfile>();
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductDtoValidator>();

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Лаб. 09: Swagger UI входить через Duende IdentityServer — Authorization Code + PKCE, публічний клієнт "swagger"
builder.Services.AddSwaggerGen(options =>
{
    var authority = PlatformAuthentication.IdentityServerAuthority(builder.Configuration);
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Duende IdentityServer (Authorization Code + PKCE)",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{authority}/connect/authorize"),
                TokenUrl = new Uri($"{authority}/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    ["openid"] = "OpenID Connect",
                    ["profile"] = "User profile",
                    ["products.read"] = "Catalog API: read",
                    ["products.write"] = "Catalog API: write"
                }
            }
        }
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] = ["products.read", "products.write"]
    });
});

var app = builder.Build();

app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();

    if (!app.Environment.IsProduction() && app.Configuration.GetValue("Seeding:Enabled", true))
        await DbInitializer.SeedAsync(db);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapGrpcHealthChecksService();
app.MapGrpcService<CatalogGrpcService>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("swagger");
        options.OAuthAppName("Catalog API");
        options.OAuthUsePkce();
        options.OAuthScopes("openid", "profile", "products.read", "products.write");
    });
}

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
