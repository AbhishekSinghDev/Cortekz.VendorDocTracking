using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class CreateSubmissionRequest
{
    [Required, MaxLength(200)]
    public string SubmittedBy { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<CreateSubmissionFileRequest> Files { get; set; } = new();

    public JsonElement? Metadata { get; set; }
}
