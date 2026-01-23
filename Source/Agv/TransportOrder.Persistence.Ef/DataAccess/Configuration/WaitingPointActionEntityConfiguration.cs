using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
public class WaitingPointActionEntityConfiguration : IEntityTypeConfiguration<WaitingPointAction>
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
    public void Configure(EntityTypeBuilder<WaitingPointAction> builder)
    {
        builder.HasKey(e => e.AgvActionCorrelationId);
        builder.Property(e => e.AgvCorrelationId);

        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(a => a.TargetNodeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.CreatedTime);
    }
}