using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class RoutingNodeEntityConfiguration : IEntityTypeConfiguration<RoutingNode>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingNode> builder)
    {
        builder.Property(e => e.Theta);

        builder.OwnsOne(e => e.LoadState, l =>
        {
            l.Property(s => s.IsLoaded).HasColumnName(nameof(LoadState.IsLoaded));
        });

        // RoutingEdge has two references to RoutingNode (StartNode/EndNode), so the inverse collections
        // must be paired explicitly - a plain HasMany() on both sides would be ambiguous.
        builder.HasMany(e => e.IncomingEdges)
            .WithOne(e => e.EndNode)
            .HasForeignKey("EndNodeId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.OutgoingEdges)
            .WithOne(e => e.StartNode)
            .HasForeignKey("StartNodeId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
