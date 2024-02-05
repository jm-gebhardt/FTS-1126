using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess.Configuration;

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