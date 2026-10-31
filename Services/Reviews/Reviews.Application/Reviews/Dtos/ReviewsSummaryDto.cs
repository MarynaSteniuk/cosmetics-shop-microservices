namespace Reviews.Application.Reviews.Dtos;

public class ReviewsSummaryDto
{
    public long ProductId { get; set; }
    public long TotalCount { get; set; }
    public double? AverageRating { get; set; }
    public List<ReviewDto> Latest { get; set; } = new();
}
