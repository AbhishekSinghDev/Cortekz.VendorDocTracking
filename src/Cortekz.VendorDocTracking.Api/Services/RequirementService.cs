using Cortekz.VendorDocTracking.Api.Data;
using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cortekz.VendorDocTracking.Api.Services;

public enum ListRequirementsOutcome
{
    Success,
    PurchaseOrderNotFound
}

public class ListRequirementsResult
{
    public ListRequirementsOutcome Outcome { get; init; }
    public PagedResult<DocumentRequirementListItemResponse>? Data { get; init; }

    public static ListRequirementsResult Success(PagedResult<DocumentRequirementListItemResponse> data) =>
        new() { Outcome = ListRequirementsOutcome.Success, Data = data };

    public static ListRequirementsResult NotFound() =>
        new() { Outcome = ListRequirementsOutcome.PurchaseOrderNotFound };
}

public class RequirementService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;
    private readonly SubmissionRepository _submissionRepository;

    public RequirementService(AppDbContext db, SubmissionRepository submissionRepository)
    {
        _db = db;
        _submissionRepository = submissionRepository;
    }

    public async Task<ListRequirementsResult> ListAsync(Guid purchaseOrderId, RequirementListQuery query)
    {
        var purchaseOrderExists = await _db.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId);
        if (!purchaseOrderExists)
        {
            return ListRequirementsResult.NotFound();
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        var requirementsQuery = _db.DocumentRequirements
            .AsNoTracking()
            .Where(r => r.PurchaseOrderId == purchaseOrderId);

        if (query.Status is not null)
        {
            requirementsQuery = requirementsQuery.Where(r => r.Status == query.Status);
        }

        if (query.DocumentType is not null)
        {
            requirementsQuery = requirementsQuery.Where(r => r.DocumentType == query.DocumentType);
        }

        if (query.DueBefore is not null)
        {
            requirementsQuery = requirementsQuery.Where(r => r.DueDate < query.DueBefore);
        }

        if (query.Overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            requirementsQuery = requirementsQuery.Where(r => r.DueDate < today && r.Status != RequirementStatus.Approved);
        }

        var totalCount = await requirementsQuery.CountAsync();

        var requirements = await requirementsQuery
            .OrderBy(r => r.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var submissionIds = requirements
            .Where(r => r.LatestSubmissionId is not null)
            .Select(r => r.LatestSubmissionId!)
            .ToList();

        var submissions = await _submissionRepository.GetByIdsAsync(submissionIds);
        var submissionsById = submissions.ToDictionary(s => s.Id.ToString());

        var items = requirements.Select(r => MapToListItem(r, submissionsById)).ToList();

        return ListRequirementsResult.Success(new PagedResult<DocumentRequirementListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private static DocumentRequirementListItemResponse MapToListItem(
        DocumentRequirement requirement,
        Dictionary<string, SubmissionDocument> submissionsById)
    {
        LatestSubmissionSummaryResponse? latestSubmission = null;
        if (requirement.LatestSubmissionId is not null &&
            submissionsById.TryGetValue(requirement.LatestSubmissionId, out var submission))
        {
            latestSubmission = new LatestSubmissionSummaryResponse
            {
                SubmissionId = submission.Id.ToString(),
                Revision = submission.Revision,
                SubmittedAt = submission.SubmittedAt,
                SubmittedBy = submission.SubmittedBy,
                ReviewStatus = submission.Review.Status,
                AiStatus = submission.AiReview.Status,
                AiVerdict = submission.AiReview.Verdict,
                AiFlaggedIssueCount = submission.AiReview.FlaggedIssues.Count
            };
        }

        return new DocumentRequirementListItemResponse
        {
            Id = requirement.Id,
            DocumentType = requirement.DocumentType,
            Title = requirement.Title,
            DueDate = requirement.DueDate,
            IsMandatory = requirement.IsMandatory,
            Status = requirement.Status,
            CurrentRevision = requirement.CurrentRevision,
            LatestSubmission = latestSubmission
        };
    }
}
