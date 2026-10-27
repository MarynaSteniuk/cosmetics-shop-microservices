using System.Diagnostics;

namespace Catalog.Bll.Telemetry;

public static class CatalogTelemetry
{
    public const string SourceName = "Cosmetics.Catalog";

    public static readonly ActivitySource Source = new(SourceName);
}
