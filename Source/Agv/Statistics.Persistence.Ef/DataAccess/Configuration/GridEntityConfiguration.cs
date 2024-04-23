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
        
        builder.HasMany(g => g.AgvGridPositions)
            .WithOne(agp => agp.Grid)
            .HasForeignKey(agp => agp.GridId);
        
        builder.HasMany(g => g.Maps)
            .WithOne(agp => agp.Grid)
            .HasForeignKey(agp => agp.GridId);
        
        builder.ConfigureAllProperties(except: new[] { nameof(Grid.AgvGridPositions), nameof(Grid.Maps) });
    }
}