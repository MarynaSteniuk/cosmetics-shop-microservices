using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Driver;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Platform.ServiceDefaults.Security;
using Reviews.Application.Common.Mappings;
using Reviews.Application;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure;
using Reviews.WebApi.ErrorHandling;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.IntegrationEvents;

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
        options.Audience = "reviews_api";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.AuthenticationType = PlatformAuthentication.IdentityServerScheme;
        options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
    })
    // Частина 3: Keycloak (Aspire.Keycloak.Authentication), realm DevRealmDrivers, відгуки перевіряються audience products-api
    .AddKeycloakJwtBearer(PlatformAuthentication.KeycloakServiceName, PlatformAuthentication.KeycloakRealm,
        PlatformAuthentication.KeycloakScheme, options =>
        {
            options.Audience = "products-api"; // відгуки належать товарам — той самий resource server Keycloak
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.AuthenticationType = PlatformAuthentication.KeycloakScheme;
            options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
            options.Events = PlatformAuthentication.KeycloakEvents();
        });

// ---------- Лаб. 09: авторизація — політики за дозволами матриці (Keycloak permissions) ----------
builder.Services.AddAuthorization(options =>
{
    // Частина 2: політика за scope IdentityServer для machine-to-machine викликів (Aggregator, Client Credentials)
    options.AddPolicy("reviews.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(PlatformAuthentication.ScopeClaimType, "reviews.read"));
});

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);

builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());
builder.Services.AddGrpcHealthChecks();

builder.Services.AddReviewsHealthChecks(builder.Configuration);

builder.AddRabbitMQClient("messaging");
builder.Services.AddReviewsIntegrationEvents();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var reviewsConnection = builder.Configuration.GetConnectionString("ReviewsDb");
if (!string.IsNullOrWhiteSpace(reviewsConnection))
{
    builder.Services.PostConfigure<MongoDbOptions>(options =>
    {
        options.ConnectionString = reviewsConnection;
        options.DatabaseName = MongoUrl.Create(reviewsConnection).DatabaseName ?? options.DatabaseName;
    });
}

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddMaps(typeof(ReviewsMappingProfile).Assembly);
});

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();
app.MapGrpcHealthChecksService();
app.MapGrpcService<ReviewsGrpcService>();

app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
