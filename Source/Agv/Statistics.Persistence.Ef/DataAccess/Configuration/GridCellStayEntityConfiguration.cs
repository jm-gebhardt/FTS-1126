using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GridCellStayEntityConfiguration : IEntityTypeConfiguration<GridCellStay>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GridCellStay> builder)
    {
        builder.ToTable("StatisticsGridCellStays");
        builder.HasKey(a => a.StayId);
        builder.Property(e => e.DurationSeconds);
        builder.Ignore(e => e.Duration);
    }
}