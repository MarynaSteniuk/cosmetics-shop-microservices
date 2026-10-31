using AutoMapper;
using Orders.Api.ErrorHandling;
using Orders.Api.Grpc;
using Orders.Bll.Caching;
using Orders.Bll.Mapping;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Dal;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing");

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddAutoMapper(cfg =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        cfg.LicenseKey = licenseKey;
    cfg.AddProfile<OrdersMappingProfile>();
});

builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();
app.MapGrpcService<OrdersGrpcService>();

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
