using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class RoutingEdgeEntityConfiguration : IEntityTypeConfiguration<RoutingEdge>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingEdge> builder)
    {
        // StartNode/EndNode <-> IncomingEdges/OutgoingEdges are configured from the RoutingNode side
        // (see RoutingNodeEntityConfiguration) since that's where both ends of each relationship meet.
        builder.Property(e => e.Length);
        builder.Property(e => e.RotationAllowed);
        builder.Property(e => e.MaxSpeed);
        builder.Property(e => e.MaxRotationSpeed);

        // Trajectory has no properties yet .
        builder.Ignore(e => e.Trajectory);
    }
}
