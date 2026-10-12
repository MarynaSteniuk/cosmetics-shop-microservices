namespace Orders.Domain.Models;

public class OrderDetails
{
    public long OrderId { get; set; }
    public string ShippingAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string? Comment { get; set; }
}
