namespace Cortekz.VendorDocTracking.Api.Models;

public class AiReviewJob
{
    public Guid Id { get; set; }
    public string SubmissionId { get; set; } = string.Empty;
    public Guid RequirementId { get; set; }
    public string? ExternalJobId { get; set; }
    public AiReviewJobStatus Status { get; set; } = AiReviewJobStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DocumentRequirement Requirement { get; set; } = null!;
}
