using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

public class OrderItemDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public int Units { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string? Comment { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderSummaryDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateOrderItemDto
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; set; }

    [Range(1, 1000)]
    public int Units { get; set; }
}

public class CreateOrderDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required, MinLength(5), MaxLength(400)]
    public string ShippingAddress { get; set; } = "";

    [Required, MinLength(2), MaxLength(100)]
    public string City { get; set; } = "";

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [MaxLength(30)]
    public string? PromoCode { get; set; }

    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class ChangeOrderStatusDto
{
    [Required, MaxLength(30)]
    public string Status { get; set; } = "";
}
