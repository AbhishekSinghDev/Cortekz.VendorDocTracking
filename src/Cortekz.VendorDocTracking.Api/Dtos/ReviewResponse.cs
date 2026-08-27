namespace Cortekz.VendorDocTracking.Api.Dtos;

public class ReviewResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public List<ReviewCommentResponse> Comments { get; set; } = new();
}
