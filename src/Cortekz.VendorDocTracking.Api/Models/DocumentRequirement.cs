namespace Cortekz.VendorDocTracking.Api.Models;

public class DocumentRequirement
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public bool IsMandatory { get; set; }
    public RequirementStatus Status { get; set; } = RequirementStatus.Pending;
    public int CurrentRevision { get; set; }
    public string? LatestSubmissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public bool CanAcceptSubmission() => Status is RequirementStatus.Pending or RequirementStatus.ResubmitRequired;

    public void RecordSubmission(string submissionId, DateTime occurredAt)
    {
        CurrentRevision += 1;
        Status = RequirementStatus.Submitted;
        LatestSubmissionId = submissionId;
        UpdatedAt = occurredAt;
    }
}
