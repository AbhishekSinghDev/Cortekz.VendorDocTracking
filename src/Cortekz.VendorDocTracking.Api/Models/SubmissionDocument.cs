using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cortekz.VendorDocTracking.Api.Models;

public class SubmissionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string RequirementId { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string VendorId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int Revision { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public List<SubmissionFile> Files { get; set; } = new();
    public BsonDocument Metadata { get; set; } = new();
    public ReviewInfo Review { get; set; } = new();
    public AiReviewInfo AiReview { get; set; } = new();
    public int SchemaVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SubmissionFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? Checksum { get; set; }
}

public class ReviewInfo
{
    public string Status { get; set; } = "Pending";
    public DateTime? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
    public List<ReviewComment> Comments { get; set; } = new();
    public List<string> Attachments { get; set; } = new();
}

public class ReviewComment
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Severity { get; set; } = "Minor";
    public DateTime CreatedAt { get; set; }
}

public class AiReviewInfo
{
    public string? JobId { get; set; }
    public string Status { get; set; } = "Queued";
    public string? Verdict { get; set; }
    public double? Confidence { get; set; }
    public List<string> FlaggedIssues { get; set; } = new();
    public DateTime? CompletedAt { get; set; }
    [BsonIgnoreIfNull]
    public BsonDocument? Raw { get; set; }
}
