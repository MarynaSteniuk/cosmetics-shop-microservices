namespace Orders.Domain.Models;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long? PromoCodeId { get; set; }
    public string Status { get; set; } = OrderStatuses.Submitted;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    public OrderDetails? Details { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
