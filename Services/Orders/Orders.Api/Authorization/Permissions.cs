namespace Orders.Api.Authorization;

/// <summary>
/// Назви дозволів (scopes Keycloak) за матрицею Лаб. 09, таблиці 3.3.1–3.3.3.
/// Ті самі назви має Pulumi-проєкт infra/keycloak.
/// </summary>
public static class Permissions
{
    public static class Orders
    {
        public const string Read = "orders:read";
        public const string Create = "orders:create";
        public const string Update = "orders:update";
        public const string Delete = "orders:delete";
        public const string Approve = "orders:approve";

        public static readonly IReadOnlyList<string> All = [Read, Create, Update, Delete, Approve];
    }

    public static class Products
    {
        public const string Read = "products:read";
        public const string Create = "products:create";
        public const string Update = "products:update";
        public const string Delete = "products:delete";
        public const string Publish = "products:publish";

        public static readonly IReadOnlyList<string> All = [Read, Create, Update, Delete, Publish];
    }

    public static class Payments
    {
        public const string Read = "payments:read";
        public const string Process = "payments:process";
        public const string Refund = "payments:refund";

        public static readonly IReadOnlyList<string> All = [Read, Process, Refund];
    }

    public static readonly IReadOnlyList<string> All = [.. Orders.All, .. Products.All, .. Payments.All];
}
