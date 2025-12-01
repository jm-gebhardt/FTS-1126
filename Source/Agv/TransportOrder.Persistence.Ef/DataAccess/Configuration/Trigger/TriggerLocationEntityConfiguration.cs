using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
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
        builder.HasMany(e => e.Polygon)
            .WithOne()
            .HasForeignKey(p => p.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Polygon).AutoInclude();
        builder.OwnsMany(e => e.ConfigurationParameters,
            o =>
            {
                // TODO: restrict lenght of: Name, Description, Value, Validation
                o.ToTable("TriggerLocationConfigurationParameters");
            });
        builder.Navigation(e => e.ConfigurationParameters).AutoInclude();
        // TODO: Use proper relation to Map table
        builder.Property(e => e.MapId);
        // TODO: Use proper relation to Edge table
        builder.Property(e => e.EdgeId);

        // TODO: Use EfNode type and HasOne relation
        builder.HasOne<GraphNode>(t => t.Node);
        //     , n =>
        // {
        //     n.Property(nn => nn.Id);
        //     n.Property(nn => nn.Name).HasMaxLength(DbStringLimits.NodeId);
        //     n.Ignore(nn => nn.TimeStamp);
        //     n.Ignore(nn => nn.Type);
        //     n.Ignore(nn => nn.X);
        //     n.Ignore(nn => nn.Y);
        //     n.Ignore(nn => nn.Z);
        //     n.Ignore(nn => nn.DropLiftHeight);
        //     n.Ignore(nn => nn.PickLiftHeight);
        //     n.Ignore(nn => nn.DockingDirection);
        //     n.Ignore(nn => nn.Theta);
        // });

        builder.Property(e => e.Agvs).HasJsonConversion();
    }
}