using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Platform.Aggregator.Security;

public interface IServiceTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct);
}

/// <summary>
/// Machine-to-machine: Aggregator отримує токен у IdentityServer (POST /connect/token, grant_type=client_credentials)
/// і тримає його в пам'яті до закінчення терміну дії мінус 60 секунд.
/// </summary>
public sealed class ClientCredentialsTokenProvider : IServiceTokenProvider, IDisposable
{
    public const string HttpClientName = "identityserver-token";
    private static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ClientCredentialsOptions _options;
    private readonly ILogger<ClientCredentialsTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public ClientCredentialsTokenProvider(IHttpClientFactory httpClientFactory,
        IOptions<ClientCredentialsOptions> options, ILogger<ClientCredentialsTokenProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt)
            return _accessToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                return _accessToken;

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_options.Authority.TrimEnd('/')}/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["scope"] = _options.Scope
                })
            };

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("IdentityServer refused client credentials for {ClientId}: {StatusCode} {Body}",
                    _options.ClientId, (int)response.StatusCode, body);
                throw new InvalidOperationException(
                    $"Cannot obtain a service token for {_options.ClientId}: {(int)response.StatusCode}");
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                ?? throw new InvalidOperationException("IdentityServer returned an empty token response");

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) - RefreshBeforeExpiry;

            _logger.LogInformation("Service token for {ClientId} issued, scope {Scope}, valid until {ExpiresAt:O}",
                _options.ClientId, token.Scope, _expiresAt);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("scope")] string? Scope);
}
