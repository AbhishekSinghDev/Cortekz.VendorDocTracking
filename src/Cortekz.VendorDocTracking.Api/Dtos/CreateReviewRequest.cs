using System.ComponentModel.DataAnnotations;
using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class CreateReviewRequest
{
    [Required]
    public ReviewDecision Decision { get; set; }

    [Required, MaxLength(200)]
    public string ReviewedBy { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string CommentText { get; set; } = string.Empty;

    public CommentSeverity Severity { get; set; } = CommentSeverity.Minor;
}
