using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cortekz.VendorDocTracking.Api.Controllers;

[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly SubmissionService _submissionService;

    public SubmissionsController(SubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpPost("{submissionId}/review")]
    public async Task<IActionResult> Review(string submissionId, [FromBody] CreateReviewRequest request)
    {
        var result = await _submissionService.RecordReviewAsync(submissionId, request);

        return result.Outcome switch
        {
            RecordReviewOutcome.Decided => Ok(result.Submission),
            RecordReviewOutcome.SubmissionNotFound =>
                NotFound(new { message = $"Submission '{submissionId}' does not exist." }),
            RecordReviewOutcome.AlreadyDecided =>
                Conflict(new { message = $"Submission '{submissionId}' has already been decided." }),
            RecordReviewOutcome.IllegalTransition =>
                Conflict(new { message = $"Submission '{submissionId}' is not in a reviewable state." }),
            _ => Problem()
        };
    }
}
