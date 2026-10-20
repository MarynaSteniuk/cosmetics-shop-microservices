using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductImage
{
    public long Id { get; private set; }
    public long ProductId { get; private set; }
    public string Url { get; private set; } = "";
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsMain { get; private set; }

    private ProductImage() { }

    internal ProductImage(string url, string? altText, int sortOrder, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Image URL is required");

        Url = url.Trim();
        AltText = altText;
        SortOrder = sortOrder;
        IsMain = isMain;
    }

    internal void UnmarkMain() => IsMain = false;
}
