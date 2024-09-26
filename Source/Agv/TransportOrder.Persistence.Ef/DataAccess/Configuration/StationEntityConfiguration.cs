using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class StationEntityConfiguration : IEntityTypeConfiguration<Station>
{
    private const int MaxLength = 200;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(MaxLength);
        builder.Property(e => e.MapName).HasMaxLength(MaxLength);
        builder.Property(e => e.LockReason).HasMaxLength(MaxLength);
        builder.Property(e => e.LockedBy).HasMaxLength(MaxLength);
        builder.Property(e => e.Width);
        builder.Property(e => e.Length);
        builder.Property(e => e.PrimaryType);
        builder.Ignore(e => e.AdditionalTypes);
        builder.Ignore(e => e.NodeLock);
        builder.Ignore(e => e.Agvs);
        builder.Ignore(e => e.IsLocked);
    }
}