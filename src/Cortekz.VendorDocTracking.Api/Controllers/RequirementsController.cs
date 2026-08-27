using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cortekz.VendorDocTracking.Api.Controllers;

[ApiController]
[Route("api/requirements")]
public class RequirementsController : ControllerBase
{
    private readonly SubmissionService _submissionService;

    public RequirementsController(SubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpPost("{requirementId:guid}/submissions")]
    public async Task<IActionResult> CreateSubmission(Guid requirementId, [FromBody] CreateSubmissionRequest request)
    {
        var result = await _submissionService.CreateAsync(requirementId, request);

        return result.Outcome switch
        {
            CreateSubmissionOutcome.Created =>
                Created($"/api/requirements/{requirementId}/submissions", result.Submission),
            CreateSubmissionOutcome.RequirementNotFound =>
                NotFound(new { message = $"Requirement '{requirementId}' does not exist." }),
            CreateSubmissionOutcome.InvalidTransition =>
                Conflict(new { message = $"Requirement '{requirementId}' is not accepting new submissions in its current state." }),
            CreateSubmissionOutcome.MongoWriteFailed =>
                StatusCode(StatusCodes.Status502BadGateway, new { message = "Submission could not be stored, please resubmit." }),
            _ => Problem()
        };
    }

    [HttpGet("{requirementId:guid}/submissions")]
    public async Task<IActionResult> GetSubmissionHistory(Guid requirementId)
    {
        var result = await _submissionService.GetHistoryAsync(requirementId);

        return result.Outcome switch
        {
            GetHistoryOutcome.Success => Ok(result.Submissions),
            GetHistoryOutcome.RequirementNotFound =>
                NotFound(new { message = $"Requirement '{requirementId}' does not exist." }),
            _ => Problem()
        };
    }
}
