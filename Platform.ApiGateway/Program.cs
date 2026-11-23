using System.Diagnostics;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Security;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---------- Лаб. 09: Gateway перевіряє JWT усіх трьох видавців до того, як запит піде в сервіс ----------
var authServiceIssuer = PlatformAuthentication.AuthServiceIssuer(builder.Configuration);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = PlatformAuthentication.MultiIssuerScheme;
        options.DefaultChallengeScheme = PlatformAuthentication.MultiIssuerScheme;
    })
    .AddPolicyScheme(PlatformAuthentication.MultiIssuerScheme, "Auth Service, IdentityServer або Keycloak", options =>
        options.ForwardDefaultSelector = context => PlatformAuthentication.SelectScheme(context, authServiceIssuer))
    .AddJwtBearer(PlatformAuthentication.AuthServiceScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = PlatformAuthentication.AuthServiceValidation(builder.Configuration);
    })
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = PlatformAuthentication.IdentityServerAuthority(builder.Configuration);
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidAudiences = ["orders_api", "products_api", "reviews_api", "payments_api"];
        options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
    })
    .AddKeycloakJwtBearer(PlatformAuthentication.KeycloakServiceName, PlatformAuthentication.KeycloakRealm,
        PlatformAuthentication.KeycloakScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidAudiences = ["orders-api", "products-api", "payments-api"];
            options.TokenValidationParameters.RoleClaimType = PlatformAuthentication.RoleClaimType;
            options.Events = PlatformAuthentication.KeycloakEvents();
        });

// Маршрути YARP: "AuthorizationPolicy": "default" — потрібен валідний токен; "anonymous" — /auth/* (login, register ...)
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms(transforms => transforms.AddRequestTransform(context =>
    {
        // Сервіси отримують ідентифікатор користувача з перевіреного токена; підробити заголовок ззовні не можна
        context.ProxyRequest.Headers.Remove("X-User-Id");
        if (context.HttpContext.User.FindFirst(PlatformAuthentication.SubjectClaimType)?.Value is { } subject)
            context.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", subject);

        return ValueTask.CompletedTask;
    }));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    await next(context);

    stopwatch.Stop();

    if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
    {
        app.Logger.LogWarning("Gateway rejected {Method} {Path}: {StatusCode} (subject {Subject}) in {ElapsedMs} ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode,
            context.User.FindFirst(PlatformAuthentication.SubjectClaimType)?.Value ?? "anonymous",
            stopwatch.ElapsedMilliseconds);
    }

    var proxy = context.Features.Get<IReverseProxyFeature>();
    if (proxy is null)
        return;

    var routeId = proxy.Route.Config.RouteId;
    var clusterId = proxy.Cluster.Config.ClusterId;
    var destination = proxy.ProxiedDestination?.Model.Config.Address;
    var error = context.Features.Get<IForwarderErrorFeature>();

    if (error is not null)
    {
        app.Logger.LogError(error.Exception,
            "Proxy error {ProxyError}: {Method} {Path} -> route {RouteId}, cluster {ClusterId}, destination {Destination}: {StatusCode} in {ElapsedMs} ms",
            error.Error, context.Request.Method, context.Request.Path, routeId, clusterId, destination,
            context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        return;
    }

    app.Logger.LogInformation(
        "{Method} {Path} -> route {RouteId}, cluster {ClusterId}, destination {Destination}: {StatusCode} in {ElapsedMs} ms, {ResponseBytes} bytes",
        context.Request.Method, context.Request.Path, routeId, clusterId, destination,
        context.Response.StatusCode, stopwatch.ElapsedMilliseconds, context.Response.ContentLength);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();
