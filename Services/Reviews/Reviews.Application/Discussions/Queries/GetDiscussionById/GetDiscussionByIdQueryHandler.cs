using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Discussions.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Discussions.Queries.GetDiscussionById;

public sealed class GetDiscussionByIdQueryHandler : IRequestHandler<GetDiscussionByIdQuery, DiscussionDto>
{
    private readonly IDiscussionRepository _discussions;
    private readonly IMapper _mapper;

    public GetDiscussionByIdQueryHandler(IDiscussionRepository discussions, IMapper mapper)
    {
        _discussions = discussions;
        _mapper = mapper;
    }

    public async Task<DiscussionDto> Handle(GetDiscussionByIdQuery query, CancellationToken cancellationToken)
    {
        var discussion = await _discussions.GetByIdAsync(ObjectId.Parse(query.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Discussion), query.Id);

        return _mapper.Map<DiscussionDto>(discussion);
    }
}
