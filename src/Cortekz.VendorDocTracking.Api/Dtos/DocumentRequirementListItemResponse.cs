using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class DocumentRequirementListItemResponse
{
    public Guid Id { get; set; }
    public DocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public bool IsMandatory { get; set; }
    public RequirementStatus Status { get; set; }
    public int CurrentRevision { get; set; }
    public LatestSubmissionSummaryResponse? LatestSubmission { get; set; }
}
