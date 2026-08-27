namespace Cortekz.MockAiReviewService.Dtos;

public class ReviewJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Verdict { get; set; }
    public double? Confidence { get; set; }
    public List<string> FlaggedIssues { get; set; } = new();
}
