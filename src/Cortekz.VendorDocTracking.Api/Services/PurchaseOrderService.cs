using Cortekz.VendorDocTracking.Api.Data;
using Cortekz.VendorDocTracking.Api.Dtos;
using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cortekz.VendorDocTracking.Api.Services;

public enum CreatePurchaseOrderOutcome
{
    Created,
    VendorNotFound,
    DuplicatePoNumber
}

public class CreatePurchaseOrderResult
{
    public CreatePurchaseOrderOutcome Outcome { get; init; }
    public PurchaseOrderResponse? PurchaseOrder { get; init; }

    public static CreatePurchaseOrderResult Success(PurchaseOrderResponse response) =>
        new() { Outcome = CreatePurchaseOrderOutcome.Created, PurchaseOrder = response };

    public static CreatePurchaseOrderResult Failure(CreatePurchaseOrderOutcome outcome) =>
        new() { Outcome = outcome };
}

public class PurchaseOrderService
{
    private readonly AppDbContext _db;

    public PurchaseOrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreatePurchaseOrderResult> CreateAsync(CreatePurchaseOrderRequest request)
    {
        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == request.VendorId);
        if (!vendorExists)
        {
            return CreatePurchaseOrderResult.Failure(CreatePurchaseOrderOutcome.VendorNotFound);
        }

        var poNumberTaken = await _db.PurchaseOrders.AnyAsync(po => po.PoNumber == request.PoNumber);
        if (poNumberTaken)
        {
            return CreatePurchaseOrderResult.Failure(CreatePurchaseOrderOutcome.DuplicatePoNumber);
        }

        var now = DateTime.UtcNow;
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = request.PoNumber,
            VendorId = request.VendorId,
            ProjectCode = request.ProjectCode,
            Title = request.Title,
            IssuedOn = request.IssuedOn,
            Status = PurchaseOrderStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            DocumentRequirements = request.DocumentRequirements.Select(r => new DocumentRequirement
            {
                Id = Guid.NewGuid(),
                DocumentType = r.DocumentType,
                Title = r.Title,
                DueDate = r.DueDate,
                IsMandatory = r.IsMandatory,
                Status = RequirementStatus.Pending,
                CurrentRevision = 0,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList()
        };

        _db.PurchaseOrders.Add(purchaseOrder);
        await _db.SaveChangesAsync();

        return CreatePurchaseOrderResult.Success(MapToResponse(purchaseOrder));
    }

    private static PurchaseOrderResponse MapToResponse(PurchaseOrder po) => new()
    {
        Id = po.Id,
        PoNumber = po.PoNumber,
        VendorId = po.VendorId,
        ProjectCode = po.ProjectCode,
        Title = po.Title,
        IssuedOn = po.IssuedOn,
        Status = po.Status,
        CreatedAt = po.CreatedAt,
        UpdatedAt = po.UpdatedAt,
        DocumentRequirements = po.DocumentRequirements.Select(r => new DocumentRequirementResponse
        {
            Id = r.Id,
            DocumentType = r.DocumentType,
            Title = r.Title,
            DueDate = r.DueDate,
            IsMandatory = r.IsMandatory,
            Status = r.Status,
            CurrentRevision = r.CurrentRevision
        }).ToList()
    };
}
