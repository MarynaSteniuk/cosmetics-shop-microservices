using System.Net;
using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class CatalogClient
{
    private readonly HttpClient _http;

    public CatalogClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProductDto?> GetProductAsync(long id, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/products/{id}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(ct);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        var products = await Task.WhenAll(ids.Select(id => GetProductAsync(id, ct)));
        return products.Where(p => p is not null).Select(p => p!).ToList();
    }
}
