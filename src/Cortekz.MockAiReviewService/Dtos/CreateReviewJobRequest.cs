using System.ComponentModel.DataAnnotations;

namespace Cortekz.MockAiReviewService.Dtos;

public class CreateReviewJobRequest
{
    [Required, MaxLength(50)]
    public string SubmissionId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string DocumentRef { get; set; } = string.Empty;
}
