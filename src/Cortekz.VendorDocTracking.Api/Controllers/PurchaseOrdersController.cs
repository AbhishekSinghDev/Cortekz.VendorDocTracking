using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cortekz.VendorDocTracking.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly RequirementService _requirementService;

    public PurchaseOrdersController(PurchaseOrderService purchaseOrderService, RequirementService requirementService)
    {
        _purchaseOrderService = purchaseOrderService;
        _requirementService = requirementService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var result = await _purchaseOrderService.CreateAsync(request);

        return result.Outcome switch
        {
            CreatePurchaseOrderOutcome.Created =>
                Created($"/api/purchase-orders/{result.PurchaseOrder!.Id}", result.PurchaseOrder),
            CreatePurchaseOrderOutcome.VendorNotFound =>
                NotFound(new { message = $"Vendor '{request.VendorId}' does not exist." }),
            CreatePurchaseOrderOutcome.DuplicatePoNumber =>
                Conflict(new { message = $"PO number '{request.PoNumber}' is already in use." }),
            _ => Problem()
        };
    }

    [HttpGet("{id:guid}/requirements")]
    public async Task<IActionResult> ListRequirements(Guid id, [FromQuery] RequirementListQuery query)
    {
        var result = await _requirementService.ListAsync(id, query);

        return result.Outcome switch
        {
            ListRequirementsOutcome.Success => Ok(result.Data),
            ListRequirementsOutcome.PurchaseOrderNotFound =>
                NotFound(new { message = $"Purchase order '{id}' does not exist." }),
            _ => Problem()
        };
    }
}
