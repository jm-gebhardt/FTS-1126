using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Statistics.Domain.Models;

namespace Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapEntityConfiguration : IEntityTypeConfiguration<Map>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.ToTable("StatisticsMap");
        builder.HasKey(m => m.MapId);
        builder.Property(m => m.GridId);
    }
}