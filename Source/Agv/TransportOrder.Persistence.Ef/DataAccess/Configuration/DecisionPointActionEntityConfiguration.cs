using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
public class DecisionPointActionEntityConfiguration : IEntityTypeConfiguration<DecisionPointAction>
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
    public void Configure(EntityTypeBuilder<DecisionPointAction> builder)
    {
        builder.HasKey(e => e.AgvActionCorrelationId);
        builder.Property(e => e.AgvCorrelationId);
        builder.Property(e => e.ConsideredStations).HasJsonConversion();

        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(a => a.TargetNodeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.EntryTime);
    }
}