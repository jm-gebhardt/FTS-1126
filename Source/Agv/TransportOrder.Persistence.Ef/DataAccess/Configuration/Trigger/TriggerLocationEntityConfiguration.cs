using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Trigger;

/// <inheritdoc /> 
public class TriggerLocationEntityConfiguration : IEntityTypeConfiguration<Location>
{
    /// <inheritdoc /> 
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable("TriggerLocations");
        builder.HasMany(l => l.Polygon)
            .WithOne()
            .HasForeignKey(p => p.LocationId);
        builder.Property(e => e.MapId);
        builder.Property(e => e.EdgeId);
        builder.Property(e => e.NodeId);
        builder.Navigation(n => n.Polygon).AutoInclude();
    }
}