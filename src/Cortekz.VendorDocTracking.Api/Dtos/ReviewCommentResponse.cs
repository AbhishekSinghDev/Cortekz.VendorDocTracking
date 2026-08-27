namespace Cortekz.VendorDocTracking.Api.Dtos;

public class ReviewCommentResponse
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
