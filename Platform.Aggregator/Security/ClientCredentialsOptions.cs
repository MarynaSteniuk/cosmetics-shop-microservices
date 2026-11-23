using System.ComponentModel.DataAnnotations;

namespace Platform.Aggregator.Security;

/// <summary>Лаб. 09, частина 2: клієнт aggregator_service у Duende IdentityServer (grant type client_credentials).</summary>
public sealed class ClientCredentialsOptions
{
    public const string SectionName = "IdentityServer";

    [Required]
    public string Authority { get; set; } = "https://localhost:5101";

    [Required]
    public string ClientId { get; set; } = "aggregator_service";

    [Required]
    public string ClientSecret { get; set; } = "";

    [Required]
    public string Scope { get; set; } = "orders.read products.read reviews.read";
}
