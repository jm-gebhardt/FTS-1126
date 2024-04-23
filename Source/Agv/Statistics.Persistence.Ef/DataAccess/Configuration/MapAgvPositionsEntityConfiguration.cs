using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapAgvPositionsEntityConfiguration : IEntityTypeConfiguration<MapAgvPositions>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapAgvPositions> builder)
    {
        builder.HasKey(mp => new { mp.MapId, mp.PositionId });
        builder.ToTable("StatisticsMapAgvPositions");
        
        builder.HasOne(mp => mp.Map)
            .WithMany(mp => mp.MapAgvPositions)
            .HasForeignKey(mp => mp.MapId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(mp => mp.AgvGridPosition)
            .WithMany(mp => mp.MapAgvPositions)
            .HasForeignKey(mp => mp.PositionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.ConfigureAllProperties(except: new[] { nameof(MapAgvPositions.AgvGridPosition), nameof(MapAgvPositions.Map) });
    }
}