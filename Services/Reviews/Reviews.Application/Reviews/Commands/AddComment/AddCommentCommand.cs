using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed record AddCommentCommand(string ReviewId, string AuthorName, string Text) : ICommand<ReviewDto>;
