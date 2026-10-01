using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class GraphSegmentEntityConfiguration : IEntityTypeConfiguration<GraphSegment>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<GraphSegment> builder)
    {
        builder.ToTable(nameof(GraphSegment));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        // ClientCascade: tracked resources are deleted together with the segment. A database cascade is not possible
        // because Station and Node both cascade into NewStationInteractionNodes (multiple cascade paths).
        builder.HasMany(e => e.Nodes)
            .WithOne()
            .HasForeignKey(n => n.GraphSegmentId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasMany(e => e.Edges)
            .WithOne()
            .HasForeignKey(e => e.GraphSegmentId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasMany(e => e.Stations)
            .WithOne()
            .HasForeignKey(s => s.GraphSegmentId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
