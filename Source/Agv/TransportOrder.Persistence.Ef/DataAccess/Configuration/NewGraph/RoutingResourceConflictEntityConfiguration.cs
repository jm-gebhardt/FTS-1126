using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
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
        builder.ToTable(nameof(RoutingResourceConflict));
        builder.HasKey(e => new { e.ResourceId, e.ConflictingResourceId });

        // Both sides target the same physical RoutingResources table (TPH), so - unlike Parent/Children -
        // these can be real, enforced foreign keys.
        builder.HasOne<RoutingResource>()
            .WithMany()
            .HasForeignKey(e => e.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RoutingResource>()
            .WithMany()
            .HasForeignKey(e => e.ConflictingResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
