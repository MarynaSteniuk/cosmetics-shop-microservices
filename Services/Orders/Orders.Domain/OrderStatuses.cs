namespace Orders.Domain;

public static class OrderStatuses
{
    public const string Submitted = "Submitted";
    public const string AwaitingValidation = "AwaitingValidation";
    public const string StockConfirmed = "StockConfirmed";
    public const string Paid = "Paid";
    public const string Shipped = "Shipped";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
        [Submitted, AwaitingValidation, StockConfirmed, Paid, Shipped, Cancelled];
}
