namespace Cortekz.MockAiReviewService.Models;

public class ReviewJob
{
    public string JobId { get; set; } = string.Empty;
    public string SubmissionId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentRef { get; set; } = string.Empty;
    public ReviewJobStatus Status { get; set; } = ReviewJobStatus.Queued;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool WillFail { get; set; }
    public string? Verdict { get; set; }
    public double? Confidence { get; set; }
    public List<string> FlaggedIssues { get; set; } = new();
}
