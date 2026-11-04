using Catalog.Grpc;
using Orders.Grpc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Resilience;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults.Correlation;
using Reviews.Grpc;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis", settings => settings.DisableHealthChecks = true);
builder.Services.AddAggregatorHealthChecks(builder.Configuration);

builder.Services.AddSingleton<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"))
    .ConfigureChannel(channel => channel.ServiceConfig = GrpcRetryPolicies.For(ResiliencePolicies.Orders))
    .AddInterceptor<GrpcClientLoggingInterceptor>()
    .AddServiceResilience(ResiliencePolicies.Orders, ClientTransport.Grpc);
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"))
    .ConfigureChannel(channel => channel.ServiceConfig = GrpcRetryPolicies.For(ResiliencePolicies.Catalog))
    .AddInterceptor<GrpcClientLoggingInterceptor>()
    .AddServiceResilience(ResiliencePolicies.Catalog, ClientTransport.Grpc);
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"))
    .ConfigureChannel(channel => channel.ServiceConfig = GrpcRetryPolicies.For(ResiliencePolicies.Reviews))
    .AddInterceptor<GrpcClientLoggingInterceptor>()
    .AddServiceResilience(ResiliencePolicies.Reviews, ClientTransport.Grpc);

builder.Services.AddHttpClient<OrdersClient>(client => client.BaseAddress = new Uri("https+http://orders-api"))
    .AddServiceResilience(ResiliencePolicies.Orders, ClientTransport.Http);
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog-api"))
    .AddServiceResilience(ResiliencePolicies.Catalog, ClientTransport.Http);
builder.Services.AddHttpClient<ReviewsClient>(client => client.BaseAddress = new Uri("https+http://reviews-api"))
    .AddServiceResilience(ResiliencePolicies.Reviews, ClientTransport.Http);

builder.Services.AddScoped<GrpcOrderAggregationService>();
builder.Services.AddScoped<HttpOrderAggregationService>();
builder.Services.AddScoped<IOrderAggregationService>(sp => sp.GetRequiredService<GrpcOrderAggregationService>());

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
