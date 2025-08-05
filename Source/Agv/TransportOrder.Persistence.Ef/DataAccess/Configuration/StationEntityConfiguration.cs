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
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.NodeId).IsCaseSensitive();
        builder.Property(e => e.MapName).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LockReason).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.LockedBy).HasMaxLength(DbStringLimits.User);
        builder.Property(e => e.LockDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Width);
        builder.Property(e => e.Length);
        builder.Property(e => e.HasPalletJam);
        builder.Property(e => e.PrimaryType);
        builder.Property(e => e.AdditionalTypes).HasJsonConversion();
        builder.Ignore(e => e.NodeLock);
        builder.Ignore(e => e.Agvs);
        builder.Ignore(e => e.IsLocked);
    }
}