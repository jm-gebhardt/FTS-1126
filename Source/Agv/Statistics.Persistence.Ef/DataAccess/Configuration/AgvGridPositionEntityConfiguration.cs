using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvGridPositionEntityConfiguration : IEntityTypeConfiguration<AgvGridPosition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvGridPosition> builder)
    {
        builder.ToTable("StatisticsGridPositions");
        builder.HasKey(a => a.PositionId);
        builder.Property(m => m.GridId);
    }
}