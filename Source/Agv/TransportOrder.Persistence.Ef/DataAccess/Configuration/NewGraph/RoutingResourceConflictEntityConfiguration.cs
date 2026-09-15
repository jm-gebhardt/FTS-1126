using Gebhardt.Acc.TransportOrder.Persistence.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class RoutingResourceConflictEntityConfiguration : IEntityTypeConfiguration<RoutingResourceConflict>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingResourceConflict> builder)
    {
        builder.HasKey(e => new { e.ResourceId, e.ConflictingResourceId });

        // Each id may reference either a RoutingNode or a RoutingEdge row - RoutingNode and RoutingEdge are mapped
        // to separate physical tables (no shared RoutingResource table), so this can't be modeled as a single,
        // enforced foreign key. Stored as plain ids and resolved explicitly by NewGraphRepository, which loads the
        // matching RoutingNode/RoutingEdge rows and attaches both directions in memory via
        // RoutingResource.AddConflictingResource.
        builder.Property(e => e.ResourceId);
        builder.Property(e => e.ConflictingResourceId);
    }
}
