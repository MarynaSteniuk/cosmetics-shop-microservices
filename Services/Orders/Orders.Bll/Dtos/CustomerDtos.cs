using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

public class CustomerDto
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(2), MaxLength(200)]
    public string FullName { get; set; } = "";

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }
}

public class UpdateCustomerDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(2), MaxLength(200)]
    public string FullName { get; set; } = "";

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }
}
