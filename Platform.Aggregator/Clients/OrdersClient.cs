using System.Net;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class OrdersClient
{
    private readonly HttpClient _http;

    public OrdersClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<OrderDto?> GetOrderAsync(long id, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/orders/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }
}
