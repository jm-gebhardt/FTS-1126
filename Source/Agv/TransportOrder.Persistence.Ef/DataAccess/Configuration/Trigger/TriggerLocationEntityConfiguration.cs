using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;

/// <inheritdoc /> 
public class TriggerLocationEntityConfiguration : IEntityTypeConfiguration<Location>
{
    /// <inheritdoc /> 
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("TriggerLocations");
        builder.HasMany(e => e.Polygon)
            .WithOne()
            .HasForeignKey(p => p.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Polygon).AutoInclude();
        builder.OwnsMany(e => e.ConfigurationParameters,
            o =>
            {
                o.ToTable("TriggerLocationConfigurationParameters");
                o.Property(p => p.Name).HasMaxLength(DbStringLimits.Name);
                o.Property(p => p.Description).HasMaxLength(DbStringLimits.Description);
                o.Property(p => p.Value).HasMaxLength(DbStringLimits.Enum);
                o.Property(p => p.Validation).HasMaxLength(DbStringLimits.Description);
            });
        builder.Navigation(e => e.ConfigurationParameters).AutoInclude();
        builder.HasOne<Map>()
            .WithMany()
            .HasForeignKey(e => e.MapId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GraphEdge>()
            .WithMany()
            .HasForeignKey(e => e.EdgeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GraphNode>(t => t.Node)
            .WithMany()
            .HasForeignKey(e => e.NodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Agvs).HasJsonConversion();
    }
}