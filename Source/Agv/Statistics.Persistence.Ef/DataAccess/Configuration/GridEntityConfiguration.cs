using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class GridEntityConfiguration : IEntityTypeConfiguration<Grid>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Grid> builder)
    {
        builder.ToTable("StatisticsGrid");
        builder.HasKey(g => g.GridId);
    }
}