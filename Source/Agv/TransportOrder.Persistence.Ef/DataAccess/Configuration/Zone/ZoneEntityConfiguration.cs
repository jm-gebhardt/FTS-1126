using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.Zone;

/// <inheritdoc />
public class ZoneEntityConfiguration:IEntityTypeConfiguration<Domain.Models.Zone.Zone>
{
    private const string TableName = "AgvZone";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Domain.Models.Zone.Zone> builder)
    {
        builder.HasKey(e => e.CorrelationId);
        builder.ToTable(TableName);
        builder.Property(e => e.ZoneId).HasMaxLength(DbStringLimits.Description);
        builder.HasMany(e => e.Polygon)
            .WithOne()
            .HasForeignKey(p => p.LocationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Polygon).AutoInclude();
    }
}