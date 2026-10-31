using AutoMapper;
using MongoDB.Driver;
using Platform.ServiceDefaults.Correlation;
using Platform.ServiceDefaults.Grpc;
using Reviews.Application.Common.Mappings;
using Reviews.Application;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure;
using Reviews.WebApi.ErrorHandling;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcServerLoggingInterceptor>());

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

builder.Services.AddHealthChecks().AddCheck<MongoDbHealthCheck>("mongodb");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();
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
