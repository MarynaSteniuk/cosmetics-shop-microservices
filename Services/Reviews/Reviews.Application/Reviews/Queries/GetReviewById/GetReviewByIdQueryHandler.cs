using MediatR;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    private readonly IReviewQueries _queries;

    public GetReviewByIdQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<ReviewDto> Handle(GetReviewByIdQuery query, CancellationToken cancellationToken)
        => await _queries.GetByIdAsync(query.Id, cancellationToken)
           ?? throw new NotFoundException(nameof(Review), query.Id);
}
