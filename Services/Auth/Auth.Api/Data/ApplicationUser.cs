using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Логічне посилання на клієнта в сервісі замовлень (Customers.Id). Задає адміністратор.</summary>
    public long? CustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
