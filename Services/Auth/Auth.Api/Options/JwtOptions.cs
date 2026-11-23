using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    /// <summary>Ключ HMAC-SHA256, щонайменше 32 символи. Задається секретом jwt-secret в AppHost, не в коді.</summary>
    [Required, MinLength(32)]
    public string SecretKey { get; set; } = "";

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; set; } = 7;
}
