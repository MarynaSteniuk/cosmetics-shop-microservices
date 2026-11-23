namespace Auth.Api.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public const string RefreshTokenCookie = "refreshToken";

    /// <summary>Публічна адреса (Gateway), з якої будуються посилання в листах.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Лише для Development: дублює refresh token у тілі відповіді, щоб його можна було перевірити в Postman по HTTP.
    /// У Production токен передається тільки в HttpOnly cookie.
    /// </summary>
    public bool ExposeRefreshTokenInBody { get; set; }
}
