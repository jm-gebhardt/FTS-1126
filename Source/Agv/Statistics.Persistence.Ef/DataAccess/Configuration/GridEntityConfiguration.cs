using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Statistics.Domain.Models;

namespace Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GridEntityConfiguration: IEntityTypeConfiguration<Grid>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Grid> builder)
    {
        builder.ToTable("StatisticsGrid");
        builder.HasKey(g => g.GridId);
    }
}