using System.Text.Json;
using System.Text.Json.Serialization;
using Catalog.Grpc;
using Orders.Grpc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults.Correlation;
using Reviews.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<OrdersService.OrdersServiceClient>(o => o.Address = new Uri("https://orders-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri("https://catalog-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o => o.Address = new Uri("https://reviews-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddHttpClient<OrdersClient>(client => client.BaseAddress = new Uri("https+http://orders-api"));
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(client => client.BaseAddress = new Uri("https+http://reviews-api"));

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
