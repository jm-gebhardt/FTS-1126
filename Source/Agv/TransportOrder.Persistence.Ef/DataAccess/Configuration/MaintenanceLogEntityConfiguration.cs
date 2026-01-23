using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc cref="IEntityTypeConfiguration{MaintenanceLog}"/>>
public class MaintenanceLogEntityConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    private const string TableName = nameof(MaintenanceLog);

    /// <inheritdoc cref="IEntityTypeConfiguration{MaintenanceLog}"/>>
    public void Configure(EntityTypeBuilder<MaintenanceLog> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AgvCorrelationId);
        builder.HasOne<Agv>()
            .WithMany()
            .HasForeignKey(e => e.AgvCorrelationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Comment).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.CreationDate);
        builder.Property(e => e.ExecutionDate);
        builder.Property(e => e.User).HasMaxLength(DbStringLimits.User);
        builder.Property(e => e.MaintenanceType).HasMaxLength(DbStringLimits.Enum);
        builder.HasIndex(e => e.AgvCorrelationId);
        // no index for MaintenanceType alone as it has low cardinality
        builder.HasIndex(e => new { e.AgvCorrelationId, e.MaintenanceType });
    }
}