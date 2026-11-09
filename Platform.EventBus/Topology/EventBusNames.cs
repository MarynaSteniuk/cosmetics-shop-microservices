namespace Platform.EventBus.Topology;

public static class EventBusNames
{
    public static class Exchanges
    {
        public const string CatalogEvents = "catalog-events";
        public const string OrdersEvents = "orders-events";
    }

    public static class Queues
    {
        public const string OrdersCatalogSync = "orders.catalog-sync";
        public const string ReviewsCatalogSync = "reviews.catalog-sync";
        public const string CatalogOrdersSync = "catalog.orders-sync";
    }

    public static class RoutingKeys
    {
        public const string ProductPriceChanged = "product.price_changed";
        public const string ProductRenamed = "product.renamed";
        public const string AllProductEvents = "product.*";
        public const string OrderStockConfirmed = "order.stock_confirmed";
        public const string AllOrderEvents = "order.*";
    }

    public static class Headers
    {
        public const string CorrelationId = "x-correlation-id";
        public const string EventType = "x-event-type";
        public const string SchemaVersion = "x-schema-version";
        public const string RetryCount = "x-retry-count";
        public const string LastError = "x-last-error";
    }

    public static string DeadLetterExchange(string exchange) => $"{exchange}.dlx";

    public static string DeadLetterQueue(string queue) => $"{queue}.dlq";
}
