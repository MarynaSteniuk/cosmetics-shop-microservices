var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword, port: 14333)
    .WithDataVolume("cosmetics-sql-data");

var ordersScript = string.Join(
    $"{Environment.NewLine}GO{Environment.NewLine}",
    new[] { "schema.sql", "procedures.sql", "seed.sql", "002_confirm_order.sql", "003_integration_events.sql" }
        .Select(file => File.ReadAllText(Path.Combine(builder.AppHostDirectory, "..", "db", "p1", file))));

var ordersDb = sql.AddDatabase("OrdersDb", "CosmeticsOrdersDb")
    .WithCreationScript(ordersScript);

var catalogDb = sql.AddDatabase("CatalogDb", "CosmeticsCatalogEfDb");

var mongo = builder.AddMongoDB("mongo", port: 27018)
    .WithDataVolume("cosmetics-mongo-data")
    .WithInitFiles(Path.Combine(builder.AppHostDirectory, "..", "db", "p3"));

var reviewsDb = mongo.AddDatabase("ReviewsDb", "cosmetics_reviews");

var redis = builder.AddRedis("redis")
    .WithDataVolume("cosmetics-redis-data");

var messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume("cosmetics-rabbitmq-data")
    .WithManagementPlugin();

var orders = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health");

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(orders)
    .WithReference(catalog)
    .WithReference(reviews)
    .WithReference(redis)
    .WaitFor(orders)
    .WaitFor(catalog)
    .WaitFor(reviews)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(orders)
    .WithReference(catalog)
    .WithReference(reviews)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
