using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected void SetETag(int version) => Response.Headers.ETag = $"\"{version}\"";

    protected static bool TryParseIfMatch(string? header, out int? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(header))
            return true;

        var value = header.Trim();
        if (value.StartsWith("W/", StringComparison.Ordinal))
            value = value[2..];
        value = value.Trim('"');

        if (!int.TryParse(value, out var parsed) || parsed < 0)
            return false;

        version = parsed;
        return true;
    }
}
