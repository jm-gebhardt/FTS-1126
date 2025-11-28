using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class StationEntityConfiguration : IEntityTypeConfiguration<Station>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        // TODO: Add relation to GraphNode and rename to NodeId
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.NodeId);
        // TODO: replace by relation to Map by using MapId
        builder.Property(e => e.MapName).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LockReason).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LockedBy).HasMaxLength(DbStringLimits.User);
        builder.Property(e => e.LockDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Width);
        builder.Property(e => e.Length);
        builder.Property(e => e.HasPalletJam);
        builder.Property(e => e.PalletJamChangedDate);
        builder.Property(e => e.PrimaryType);
        builder.Property(e => e.AdditionalTypes).HasJsonConversion();
        // TODO: add proper relation to NodeLock 
        builder.Ignore(e => e.NodeLock);
        builder.Ignore(e => e.Agvs);
        builder.Ignore(e => e.IsLocked);
    }
}