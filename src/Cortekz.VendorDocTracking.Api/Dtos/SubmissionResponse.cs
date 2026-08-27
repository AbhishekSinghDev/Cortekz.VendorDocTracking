using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class SubmissionResponse
{
    public string Id { get; set; } = string.Empty;
    public Guid RequirementId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid VendorId { get; set; }
    public DocumentType DocumentType { get; set; }
    public int Revision { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public List<SubmissionFileResponse> Files { get; set; } = new();
    public ReviewResponse Review { get; set; } = new();
    public AiReviewResponse AiReview { get; set; } = new();
}
