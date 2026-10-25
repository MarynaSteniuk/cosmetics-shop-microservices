using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("ReviewId must be a valid ObjectId");
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}
