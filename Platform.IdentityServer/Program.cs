using Platform.IdentityServer;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

await SeedData.EnsureSeedDataAsync(app);

app.Run();
