using Cortekz.VendorDocTracking.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortekz.VendorDocTracking.Api.Data.Configurations;

public class AiReviewJobConfiguration : IEntityTypeConfiguration<AiReviewJob>
{
    public void Configure(EntityTypeBuilder<AiReviewJob> builder)
    {
        builder.ToTable("ai_review_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.SubmissionId).HasColumnName("submission_id").HasMaxLength(50).IsRequired();
        builder.Property(j => j.RequirementId).HasColumnName("requirement_id");
        builder.Property(j => j.ExternalJobId).HasColumnName("external_job_id").HasMaxLength(100);
        builder.Property(j => j.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(j => j.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(j => j.LastError).HasColumnName("last_error");
        builder.Property(j => j.CreatedAt).HasColumnName("created_at");
        builder.Property(j => j.UpdatedAt).HasColumnName("updated_at");
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(j => j.SubmissionId).IsUnique();
        builder.HasIndex(j => new { j.Status, j.NextAttemptAt });

        builder.HasOne(j => j.Requirement)
            .WithMany()
            .HasForeignKey(j => j.RequirementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
