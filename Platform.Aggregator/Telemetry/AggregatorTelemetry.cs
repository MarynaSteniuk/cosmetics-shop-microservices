using System.Diagnostics;

namespace Platform.Aggregator.Telemetry;

public static class AggregatorTelemetry
{
    public const string SourceName = "Platform.Aggregator";

    public static readonly ActivitySource Source = new(SourceName);
}
