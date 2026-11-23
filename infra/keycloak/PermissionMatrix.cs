namespace Cosmetics.Infra;

/// <summary>
/// Матриця доступу Лаб. 09 (таблиці 3.3.1–3.3.3). Звідси будуються:
/// client roles і authorization scopes resource server, role policies, scope permissions і composite realm roles.
/// Назви дозволів збігаються з Permissions.cs у Orders.Api, Catalog.Api і Payments.Api.
/// </summary>
public static class PermissionMatrix
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string User = "user";
    public const string Guest = "guest";

    public static readonly IReadOnlyList<string> RealmRoles = [Admin, Manager, User, Guest];

    public static readonly IReadOnlyList<ResourceServerDefinition> ResourceServers =
    [
        new("orders-api", "orders", "Orders API", "/api/orders/*", "orders_mgmt", new Dictionary<string, string[]>
        {
            ["orders:read"] = [Admin, Manager, User],
            ["orders:create"] = [Admin, Manager, User],
            ["orders:update"] = [Admin, Manager],
            ["orders:delete"] = [Admin],
            ["orders:approve"] = [Admin, Manager]
        }),
        new("products-api", "products", "Catalog (Products) API", "/api/products/*", "products_mgmt", new Dictionary<string, string[]>
        {
            ["products:read"] = [Admin, Manager, User, Guest],
            ["products:create"] = [Admin, Manager],
            ["products:update"] = [Admin, Manager],
            ["products:delete"] = [Admin],
            ["products:publish"] = [Admin, Manager]
        }),
        new("payments-api", "payments", "Payments API", "/api/payments/*", "payments_mgmt", new Dictionary<string, string[]>
        {
            ["payments:read"] = [Admin],
            ["payments:process"] = [Admin],
            ["payments:refund"] = [Admin]
        })
    ];

    /// <summary>Тестові користувачі: по одному на кожну роль.</summary>
    public static readonly IReadOnlyList<TestUser> Users =
    [
        new("alice", "Alice", "Admin", "alice@cosmetics.local", Admin),
        new("bob", "Bob", "Manager", "bob@cosmetics.local", Manager),
        new("carol", "Carol", "Customer", "carol@cosmetics.local", User),
        new("dave", "Dave", "Guest", "dave@cosmetics.local", Guest)
    ];

    /// <summary>Machine-to-machine клієнт Aggregator: лише читання замовлень і товарів.</summary>
    public static readonly IReadOnlyList<(string ResourceServer, string Permission)> AggregatorPermissions =
    [
        ("orders-api", "orders:read"),
        ("products-api", "products:read")
    ];
}

public sealed record ResourceServerDefinition(
    string ClientId,
    string ResourceName,
    string DisplayName,
    string Uri,
    string ClientScopeName,
    IReadOnlyDictionary<string, string[]> ScopeRoles);

public sealed record TestUser(string Username, string FirstName, string LastName, string Email, string RealmRole);
