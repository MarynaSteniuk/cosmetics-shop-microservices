using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Options;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return Created($"/users/{result.UserId}", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ClientIp(), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result.Response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request, CancellationToken ct)
    {
        var token = request?.RefreshToken ?? Request.Cookies[AuthOptions.RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(token))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token",
                detail: "Refresh token cookie is missing");

        var result = await _auth.RefreshAsync(token, ClientIp(), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result.Response);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RevokeTokenRequest? request, CancellationToken ct)
    {
        var token = request?.RefreshToken ?? Request.Cookies[AuthOptions.RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(token))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Token required",
                detail: "Pass refreshToken in the body or in the cookie");

        await _auth.RevokeAsync(token, CurrentUserId(), User.IsInRole(AppRoles.Admin), "Revoked by request", ct);

        if (token == Request.Cookies[AuthOptions.RefreshTokenCookie])
            DeleteRefreshTokenCookie();

        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RevokeTokenRequest? request, CancellationToken ct)
    {
        await _auth.LogoutAsync(CurrentUserId(),
            request?.RefreshToken ?? Request.Cookies[AuthOptions.RefreshTokenCookie], ct);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        await _auth.VerifyEmailAsync(request.UserId, request.Token, ct);
        return Ok(new MessageResponse("Email confirmed. You can sign in now."));
    }

    /// <summary>Те саме підтвердження за посиланням із листа (GET, щоб його можна було відкрити в браузері).</summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public Task<ActionResult<MessageResponse>> VerifyEmailLink([FromQuery] string userId, [FromQuery] string token,
        CancellationToken ct)
        => VerifyEmail(new VerifyEmailRequest(userId, token), ct);

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<MessageResponse>> ResendVerification(EmailRequest request, CancellationToken ct)
    {
        await _auth.ResendVerificationAsync(request.Email, ct);
        return Accepted(new MessageResponse("If the email is registered and not confirmed, a new link was sent."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(EmailRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request.Email, ct);
        return Accepted(new MessageResponse("If the email is registered, a password reset link was sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(request, ct);
        DeleteRefreshTokenCookie();
        return Ok(new MessageResponse("Password changed. All sessions were revoked, sign in again."));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(CurrentUserId(), request, ct);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken ct)
        => Ok(await _auth.GetCurrentUserAsync(CurrentUserId(), ct));

    private string CurrentUserId() => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                      ?? throw new InvalidOperationException("Token has no sub claim");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
        => Response.Cookies.Append(AuthOptions.RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/auth",
            Expires = expiresAt,
            IsEssential = true
        });

    private void DeleteRefreshTokenCookie()
        => Response.Cookies.Delete(AuthOptions.RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/auth"
        });
}
