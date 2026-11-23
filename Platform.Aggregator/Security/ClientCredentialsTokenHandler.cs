using System.Net.Http.Headers;

namespace Platform.Aggregator.Security;

/// <summary>
/// Додає до кожного виклику Orders / Catalog / Reviews (gRPC і HTTP) токен Client Credentials.
/// Токен користувача, що прийшов через Gateway, далі не передається — сервіси бачать Aggregator як окремий клієнт.
/// </summary>
public sealed class ClientCredentialsTokenHandler : DelegatingHandler
{
    private readonly IServiceTokenProvider _tokens;

    public ClientCredentialsTokenHandler(IServiceTokenProvider tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}
