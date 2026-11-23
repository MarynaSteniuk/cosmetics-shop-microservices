using Microsoft.AspNetCore.Authorization;
using Payments.Api.Authorization;
using Payments.Api.Services;
using Platform.ServiceDefaults.Correlation;
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
        options.Audience = "payments_api";
        options.MapInboundClaims = false;
        options.TokenValidationParameters.AuthenticationType = PlatformAuthentication.IdentityServerScheme;
        options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
    })
    // Частина 3: Keycloak (Aspire.Keycloak.Authentication), realm DevRealmDrivers, resource server payments-api
    .AddKeycloakJwtBearer(PlatformAuthentication.KeycloakServiceName, PlatformAuthentication.KeycloakRealm,
        PlatformAuthentication.KeycloakScheme, options =>
        {
            options.Audience = "payments-api";
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

});
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
