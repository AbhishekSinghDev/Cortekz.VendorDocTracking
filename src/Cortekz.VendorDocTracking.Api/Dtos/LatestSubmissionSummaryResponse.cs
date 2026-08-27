namespace Cortekz.VendorDocTracking.Api.Dtos;

public class LatestSubmissionSummaryResponse
{
    public string SubmissionId { get; set; } = string.Empty;
    public int Revision { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
}
