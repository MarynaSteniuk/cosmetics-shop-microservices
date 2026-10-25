using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class DataSeedingService : IHostedService
{
    private readonly IEnumerable<IDataSeeder> _seeders;
    private readonly MongoDbOptions _options;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(IEnumerable<IDataSeeder> seeders, IOptions<MongoDbOptions> options,
        ILogger<DataSeedingService> logger)
    {
        _seeders = seeders;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.SeedData)
        {
            _logger.LogInformation("MongoDB seeding is disabled (MongoDb:SeedData = false)");
            return;
        }

        foreach (var seeder in _seeders)
            await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
