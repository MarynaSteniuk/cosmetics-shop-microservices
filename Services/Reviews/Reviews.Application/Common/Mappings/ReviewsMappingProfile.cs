using AutoMapper;
using Reviews.Application.Discussions.Dtos;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Application.Common.Mappings;

public class ReviewsMappingProfile : Profile
{
    public ReviewsMappingProfile()
    {
        CreateMap<Comment, CommentDto>();

        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.AuthorCustomerId, o => o.MapFrom(s => s.Author.CustomerId))
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Author.DisplayName))
            .ForMember(d => d.SkinType, o => o.MapFrom(s => s.Author.SkinType))
            .ForMember(d => d.Rating, o => o.MapFrom(s => s.Rating.Value))
            .ForMember(d => d.UsageDuration, o => o.MapFrom(s => s.UsageDuration == null ? null : s.UsageDuration.ToString()))
            .ForMember(d => d.CommentsCount, o => o.MapFrom(s => s.Comments.Count))
            .ForMember(d => d.DiscussionId, o => o.MapFrom(s => s.DiscussionId.HasValue ? s.DiscussionId.Value.ToString() : null));

        CreateMap<MessagePreview, MessagePreviewDto>()
            .ForMember(d => d.MessageId, o => o.MapFrom(s => s.MessageId.ToString()));

        CreateMap<Discussion, DiscussionDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy.DisplayName));
    }
}
