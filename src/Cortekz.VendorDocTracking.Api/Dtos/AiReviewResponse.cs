namespace Cortekz.VendorDocTracking.Api.Dtos;

public class AiReviewResponse
{
    public string Status { get; set; } = string.Empty;
    public string? Verdict { get; set; }
    public double? Confidence { get; set; }
    public List<string> FlaggedIssues { get; set; } = new();
    public DateTime? CompletedAt { get; set; }
}
