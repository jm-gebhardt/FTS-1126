using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapAgvEntityConfiguration : IEntityTypeConfiguration<MapAgv>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapAgv> builder)
    {
        builder.HasKey(ma => new { ma.MapId, ma.AgvId });
        builder.ToTable("MapAgvs");
        builder.HasOne(ma => ma.Agv)
            .WithMany()
            .HasForeignKey(ma => ma.AgvId);
    }
}