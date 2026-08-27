using System.ComponentModel.DataAnnotations;
using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class CreateDocumentRequirementRequest
{
    [Required]
    public DocumentType DocumentType { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateOnly DueDate { get; set; }

    public bool IsMandatory { get; set; }
}
