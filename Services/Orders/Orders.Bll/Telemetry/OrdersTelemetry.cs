using System.Diagnostics;

namespace Orders.Bll.Telemetry;

public static class OrdersTelemetry
{
    public const string SourceName = "Cosmetics.Orders";

    public static readonly ActivitySource Source = new(SourceName);
}
