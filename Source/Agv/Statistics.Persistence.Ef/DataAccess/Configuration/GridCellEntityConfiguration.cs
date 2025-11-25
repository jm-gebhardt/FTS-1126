using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GridCellEntityConfiguration : IEntityTypeConfiguration<GridCell>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GridCell> builder)
    {
        builder.ToTable("StatisticsGridCells");
        builder.HasKey(a => a.CellId);
        builder.Property(m => m.GridId);
        builder.Property(c => c.XMin).HasPrecision(6, 2);
        builder.Property(c => c.XMax).HasPrecision(6, 2);
        builder.HasIndex(c => c.XIndex);
        builder.Property(c => c.XIndex).HasPrecision(6, 2);
        builder.Property(c => c.YMin).HasPrecision(6, 2);
        builder.Property(c => c.YMax).HasPrecision(6, 2);
        builder.HasIndex(c => c.YIndex);
        builder.Property(c => c.YIndex).HasPrecision(6, 2);

        builder.HasMany(g => g.AgvErrors)
            .WithOne(agp => agp.GridCell)
            .HasForeignKey(agp => agp.GridCellId);

        builder.HasMany(g => g.AgvFieldViolations)
            .WithOne(agp => agp.GridCell)
            .HasForeignKey(agp => agp.GridCellId);

        builder.HasMany(g => g.AgvWifiMetrics)
            .WithOne(agp => agp.GridCell)
            .HasForeignKey(agp => agp.GridCellId);

        builder.HasMany(g => g.GridCellStays)
            .WithOne(agp => agp.GridCell)
            .HasForeignKey(agp => agp.GridCellId);

        builder.ConfigureAllProperties(except: [nameof(GridCell.MapGridCell), nameof(GridCell.Grid), nameof(GridCell.AgvErrors), nameof(GridCell.AgvFieldViolations), nameof(GridCell.AgvWifiMetrics), nameof(GridCell.GridCellStays)]);
    }
}