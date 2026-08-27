namespace Cortekz.VendorDocTracking.Api.Models;

public enum PurchaseOrderStatus
{
    Draft,
    Issued,
    Closed,
    Cancelled
}

public enum DocumentType
{
    Datasheet,
    Drawing,
    TestCertificate,
    Other
}

public enum RequirementStatus
{
    Pending,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    ResubmitRequired
}

public enum AiReviewJobStatus
{
    Pending,
    Submitted,
    Processing,
    Completed,
    Failed,
    Abandoned
}

public enum ReviewDecision
{
    Approved,
    Rejected,
    ResubmitRequired
}

public enum CommentSeverity
{
    Minor,
    Major,
    Critical
}
