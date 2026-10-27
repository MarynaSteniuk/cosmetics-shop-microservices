using System.Diagnostics;

namespace Reviews.Application.Common.Telemetry;

public static class ReviewsTelemetry
{
    public const string SourceName = "Cosmetics.Reviews";

    public static readonly ActivitySource Source = new(SourceName);
}
