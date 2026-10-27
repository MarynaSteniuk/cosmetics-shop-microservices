using System.Diagnostics;
using Platform.ServiceDefaults.Correlation;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    await next(context);

    stopwatch.Stop();

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

app.MapDefaultEndpoints();
app.MapReverseProxy();

app.Run();
