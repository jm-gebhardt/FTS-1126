using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapGridCellEntityConfiguration : IEntityTypeConfiguration<MapGridCell>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapGridCell> builder)
    {
        builder.HasKey(mp => new { mp.MapId, mp.GridCellId });
        builder.ToTable("StatisticsMapGridCells");

        builder.HasOne(mp => mp.Map)
            .WithMany(mp => mp.MapGridCells)
            .HasForeignKey(mp => mp.MapId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(mp => mp.GridCell)
            .WithMany(mp => mp.MapGridCell)
            .HasForeignKey(mp => mp.GridCellId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAllProperties(except: [nameof(MapGridCell.GridCell), nameof(MapGridCell.Map)]);
    }
}