using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortekz.VendorDocTracking.Api.Data.Configurations;

public class DocumentRequirementConfiguration : IEntityTypeConfiguration<DocumentRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentRequirement> builder)
    {
        builder.ToTable("document_requirements");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(r => r.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(r => r.DueDate).HasColumnName("due_date");
        builder.Property(r => r.IsMandatory).HasColumnName("is_mandatory");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.CurrentRevision).HasColumnName("current_revision").HasDefaultValue(0);
        builder.Property(r => r.LatestSubmissionId).HasColumnName("latest_submission_id");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => new { r.PurchaseOrderId, r.DocumentType, r.Title }).IsUnique();
        builder.HasIndex(r => new { r.PurchaseOrderId, r.Status });

        builder.HasOne(r => r.PurchaseOrder)
            .WithMany(po => po.DocumentRequirements)
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
