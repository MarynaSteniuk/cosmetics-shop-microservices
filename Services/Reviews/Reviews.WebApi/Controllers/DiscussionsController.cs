using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Discussions.Dtos;
using Reviews.Application.Discussions.Queries.GetDiscussionById;

namespace Reviews.WebApi.Controllers;

[Route("api/discussions")]
public sealed class DiscussionsController : ApiControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType<DiscussionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscussionDto>> GetById(string id, CancellationToken ct)
    {
        var discussion = await Sender.Send(new GetDiscussionByIdQuery(id), ct);
        SetETag(discussion.Version);
        return Ok(discussion);
    }
}
