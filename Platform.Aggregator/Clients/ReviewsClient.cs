using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class ReviewsClient
{
    private const int PageSize = 50;

    private readonly HttpClient _http;

    public ReviewsClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResultDto<ReviewDto>> GetForProductAsync(long productId, CancellationToken ct)
        => await _http.GetFromJsonAsync<PagedResultDto<ReviewDto>>(
               $"/api/reviews?productId={productId}&page=1&pageSize={PageSize}", ct)
           ?? new PagedResultDto<ReviewDto>([], 0, 1, PageSize);

    public async Task<IReadOnlyDictionary<long, PagedResultDto<ReviewDto>>> GetForProductsAsync(
        IReadOnlyCollection<long> productIds, CancellationToken ct)
    {
        var results = await Task.WhenAll(productIds.Select(async id => (Id: id, Page: await GetForProductAsync(id, ct))));
        return results.ToDictionary(r => r.Id, r => r.Page);
    }
}
