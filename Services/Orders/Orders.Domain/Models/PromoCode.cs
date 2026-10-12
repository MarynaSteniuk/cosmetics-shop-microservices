namespace Orders.Domain.Models;

public class PromoCode
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public decimal DiscountPercent { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}
