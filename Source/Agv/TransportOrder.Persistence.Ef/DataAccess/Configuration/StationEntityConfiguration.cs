using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class StationEntityConfiguration : IEntityTypeConfiguration<Station>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne<GraphNode>()
            .WithMany()
            .HasForeignKey(e => e.Id)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.HasOne<Map>()
            .WithMany()
            .HasForeignKey(e => e.MapId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.LockReason).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LockedBy).HasMaxLength(DbStringLimits.User);
        builder.Property(e => e.LockDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Width);
        builder.Property(e => e.Length);
        builder.Property(e => e.HasPalletJam);
        builder.Property(e => e.PalletJamChangedDate);
        builder.Property(e => e.PrimaryType);
        builder.Property(e => e.AdditionalTypes).HasJsonConversion();
        builder.Property(e => e.StationGroup).HasJsonConversion();
        builder.Ignore(e => e.NodeLock);
        builder.HasOne<NodeLock>(e => e.NodeLock)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(e => e.Agvs);
        builder.Ignore(e => e.IsLocked);
    }
}