using Cortekz.VendorDocTracking.Api.Data;
using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Cortekz.VendorDocTracking.Api.Services;

public enum CreateSubmissionOutcome
{
    Created,
    RequirementNotFound,
    InvalidTransition,
    MongoWriteFailed
}

public class CreateSubmissionResult
{
    public CreateSubmissionOutcome Outcome { get; init; }
    public SubmissionResponse? Submission { get; init; }

    public static CreateSubmissionResult Success(SubmissionResponse submission) =>
        new() { Outcome = CreateSubmissionOutcome.Created, Submission = submission };

    public static CreateSubmissionResult Failure(CreateSubmissionOutcome outcome) =>
        new() { Outcome = outcome };
}

public class SubmissionService
{
    private readonly AppDbContext _db;
    private readonly SubmissionRepository _submissionRepository;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(AppDbContext db, SubmissionRepository submissionRepository, ILogger<SubmissionService> logger)
    {
        _db = db;
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<CreateSubmissionResult> CreateAsync(Guid requirementId, CreateSubmissionRequest request)
    {
        var requirement = await _db.DocumentRequirements
            .Include(r => r.PurchaseOrder)
            .FirstOrDefaultAsync(r => r.Id == requirementId);

        if (requirement is null)
        {
            return CreateSubmissionResult.Failure(CreateSubmissionOutcome.RequirementNotFound);
        }

        if (!requirement.CanAcceptSubmission())
        {
            return CreateSubmissionResult.Failure(CreateSubmissionOutcome.InvalidTransition);
        }

        var now = DateTime.UtcNow;
        var submissionId = ObjectId.GenerateNewId();

        requirement.RecordSubmission(submissionId.ToString(), now);

        var job = new AiReviewJob
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId.ToString(),
            RequirementId = requirement.Id,
            Status = AiReviewJobStatus.Pending,
            AttemptCount = 0,
            NextAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AiReviewJobs.Add(job);
        await _db.SaveChangesAsync();

        var document = new SubmissionDocument
        {
            Id = submissionId,
            RequirementId = requirement.Id.ToString(),
            PurchaseOrderId = requirement.PurchaseOrderId.ToString(),
            VendorId = requirement.PurchaseOrder.VendorId.ToString(),
            DocumentType = requirement.DocumentType.ToString(),
            Revision = requirement.CurrentRevision,
            SubmittedAt = now,
            SubmittedBy = request.SubmittedBy,
            Files = request.Files.Select(f => new SubmissionFile
            {
                FileName = f.FileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                StorageKey = f.StorageKey,
                Checksum = f.Checksum
            }).ToList(),
            Metadata = request.Metadata is null
                ? new BsonDocument()
                : BsonDocument.Parse(request.Metadata.Value.GetRawText()),
            Review = new ReviewInfo { Status = "Pending" },
            AiReview = new AiReviewInfo { Status = "Queued" },
            SchemaVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _submissionRepository.InsertAsync(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write submission {SubmissionId} for requirement {RequirementId} to Mongo",
                submissionId, requirement.Id);

            job.Status = AiReviewJobStatus.Failed;
            job.LastError = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return CreateSubmissionResult.Failure(CreateSubmissionOutcome.MongoWriteFailed);
        }

        return CreateSubmissionResult.Success(MapToResponse(document));
    }

    private static SubmissionResponse MapToResponse(SubmissionDocument document) => new()
    {
        Id = document.Id.ToString(),
        RequirementId = Guid.Parse(document.RequirementId),
        PurchaseOrderId = Guid.Parse(document.PurchaseOrderId),
        VendorId = Guid.Parse(document.VendorId),
        DocumentType = Enum.Parse<DocumentType>(document.DocumentType),
        Revision = document.Revision,
        SubmittedAt = document.SubmittedAt,
        SubmittedBy = document.SubmittedBy,
        Files = document.Files.Select(f => new SubmissionFileResponse
        {
            FileName = f.FileName,
            ContentType = f.ContentType,
            SizeBytes = f.SizeBytes,
            StorageKey = f.StorageKey,
            Checksum = f.Checksum
        }).ToList(),
        Review = new ReviewResponse
        {
            Status = document.Review.Status,
            DecidedAt = document.Review.DecidedAt,
            DecidedBy = document.Review.DecidedBy,
            Comments = document.Review.Comments.Select(c => new ReviewCommentResponse
            {
                Author = c.Author,
                Text = c.Text,
                Severity = c.Severity,
                CreatedAt = c.CreatedAt
            }).ToList()
        },
        AiReview = new AiReviewResponse
        {
            Status = document.AiReview.Status,
            Verdict = document.AiReview.Verdict,
            Confidence = document.AiReview.Confidence,
            FlaggedIssues = document.AiReview.FlaggedIssues,
            CompletedAt = document.AiReview.CompletedAt
        }
    };
}
