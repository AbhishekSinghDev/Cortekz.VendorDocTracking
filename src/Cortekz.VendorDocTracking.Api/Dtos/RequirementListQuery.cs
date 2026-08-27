using Cortekz.VendorDocTracking.Api.Models;

namespace Cortekz.VendorDocTracking.Api.Dtos;

public class RequirementListQuery
{
    public RequirementStatus? Status { get; set; }
    public DocumentType? DocumentType { get; set; }
    public DateOnly? DueBefore { get; set; }
    public bool? Overdue { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
