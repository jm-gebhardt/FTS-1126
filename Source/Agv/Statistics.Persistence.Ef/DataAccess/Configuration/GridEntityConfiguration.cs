using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GridEntityConfiguration : IEntityTypeConfiguration<Grid>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Grid> builder)
    {
        builder.ToTable("StatisticsGrid");
        builder.HasKey(g => g.GridId);
        builder.Property(g => g.GridName).HasMaxLength(DbStringLimits.Description);
        builder.Property(g => g.XStep).HasPrecision(6, 2);
        builder.Property(g => g.YStep).HasPrecision(6, 2);
        
        builder.HasMany(g => g.GridCells)
            .WithOne(agp => agp.Grid)
            .HasForeignKey(agp => agp.GridId);
        
        builder.HasMany(g => g.Maps)
            .WithOne(agp => agp.Grid)
            .HasForeignKey(agp => agp.GridId);
        
        builder.ConfigureAllProperties(except: [nameof(Grid.GridCells), nameof(Grid.Maps), nameof(Grid.GridName)]);
    }
}