var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);
var keycloakAdmin = builder.AddParameter("keycloak-admin", "admin");
var keycloakPassword = builder.AddParameter("keycloak-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword, port: 14333)
    .WithDataVolume("cosmetics-sql-data");

var ordersScript = string.Join(
    $"{Environment.NewLine}GO{Environment.NewLine}",
    new[] { "schema.sql", "procedures.sql", "seed.sql", "002_confirm_order.sql", "003_integration_events.sql" }
        .Select(file => File.ReadAllText(Path.Combine(builder.AppHostDirectory, "..", "db", "p1", file))));

var ordersDb = sql.AddDatabase("OrdersDb", "CosmeticsOrdersDb")
    .WithCreationScript(ordersScript);

var catalogDb = sql.AddDatabase("CatalogDb", "CosmeticsCatalogEfDb");

// Лаб. 09, частина 1: база ASP.NET Identity сервісу автентифікації
var identityDb = sql.AddDatabase("IdentityDb", "CosmeticsIdentityDb");

// Лаб. 09, частина 2: окремий SQL Server для IdentityServer (Configuration, Operational і Identity-таблиці)
var identityServerSql = builder.AddSqlServer("identityserver-sql", sqlPassword, port: 14334)
    .WithDataVolume("cosmetics-identityserver-sql-data");

var identityServerDb = identityServerSql.AddDatabase("IdentityServerDb", "CosmeticsIdentityServerDb");

var mongo = builder.AddMongoDB("mongo", port: 27018)
    .WithDataVolume("cosmetics-mongo-data")
    .WithInitFiles(Path.Combine(builder.AppHostDirectory, "..", "db", "p3"));

var reviewsDb = mongo.AddDatabase("ReviewsDb", "cosmetics_reviews");

var redis = builder.AddRedis("redis")
    .WithDataVolume("cosmetics-redis-data");

var messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume("cosmetics-rabbitmq-data")
    .WithManagementPlugin();

// Лаб. 09, частина 3: Keycloak на фіксованому порту 8080; realm, clients і ролі створює Pulumi (infra/keycloak)
var keycloak = builder.AddKeycloak("keycloak", 8080, keycloakAdmin, keycloakPassword)
    .WithDataVolume("cosmetics-keycloak-data");

var auth = builder.AddProject<Projects.Auth_Api>("auth-api")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithAuthServiceJwt(jwtSecret)
    .WithHttpHealthCheck("/health");

var identityServer = builder.AddProject<Projects.Platform_IdentityServer>("identityserver")
    .WithReference(identityServerDb)
    .WaitFor(identityServerDb)
    .WithHttpHealthCheck("/health");

var orders = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithPlatformAuth(jwtSecret, identityServer, keycloak)
    .WithHttpHealthCheck("/health");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithPlatformAuth(jwtSecret, identityServer, keycloak)
    .WithHttpHealthCheck("/health");

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WaitFor(reviewsDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithPlatformAuth(jwtSecret, identityServer, keycloak)
    .WithHttpHealthCheck("/health");

var payments = builder.AddProject<Projects.Payments_Api>("payments-api")
    .WithPlatformAuth(jwtSecret, identityServer, keycloak)
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
    .WithIdentityServerAuthority(identityServer)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(auth)
    .WithReference(orders)
    .WithReference(catalog)
    .WithReference(reviews)
    .WithReference(payments)
    .WithReference(aggregator)
    .WithPlatformAuth(jwtSecret, identityServer, keycloak)
    .WaitFor(auth)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
