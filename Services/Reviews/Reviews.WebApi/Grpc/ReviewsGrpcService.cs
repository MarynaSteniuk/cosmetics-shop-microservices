using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsSummary;
using Reviews.Grpc;

namespace Reviews.WebApi.Grpc;

public sealed class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private const int MaxProductsPerRequest = 100;
    private const int DefaultLatestCount = 3;
    private const int MaxLatestCount = 10;

    private readonly ISender _sender;

    public ReviewsGrpcService(ISender sender)
    {
        _sender = sender;
    }

    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        if (!ObjectId.TryParse(request.Id, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Review id must be a valid ObjectId"));

        try
        {
            var review = await _sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
            return ToReply(review);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ReviewsSummaryReply> GetReviewsSummaryForProducts(GetReviewsSummaryRequest request,
        ServerCallContext context)
    {
        if (request.ProductIds.Count == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one product id is required"));

        if (request.ProductIds.Count > MaxProductsPerRequest)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"No more than {MaxProductsPerRequest} product ids per request"));

        if (request.ProductIds.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ids must be positive"));

        var latestCount = request.LatestCount == 0 ? DefaultLatestCount : request.LatestCount;
        if (latestCount is < 1 or > MaxLatestCount)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"latest_count must be between 1 and {MaxLatestCount}"));

        var reply = new ReviewsSummaryReply();

        try
        {
            foreach (var productId in request.ProductIds.Distinct())
            {
                var summary = await _sender.Send(new GetReviewsSummaryQuery(productId, latestCount),
                    context.CancellationToken);
                reply.Summaries.Add(ToReply(summary));
            }
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }

        return reply;
    }

    private static ProductReviewsSummary ToReply(ReviewsSummaryDto summary)
    {
        var reply = new ProductReviewsSummary
        {
            ProductId = summary.ProductId,
            TotalCount = summary.TotalCount
        };

        if (summary.AverageRating is { } average)
            reply.AverageRating = average;

        reply.Latest.AddRange(summary.Latest.Select(ToReply));
        return reply;
    }

    private static ReviewReply ToReply(ReviewDto review)
    {
        var reply = new ReviewReply
        {
            Id = review.Id,
            ProductId = review.ProductId,
            AuthorName = review.AuthorName,
            Rating = review.Rating,
            Text = review.Text,
            CreatedAt = Timestamp.FromDateTime(ToUtc(review.CreatedAt))
        };

        if (review.Title is not null)
            reply.Title = review.Title;

        return reply;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
