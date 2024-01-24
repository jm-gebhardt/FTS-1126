using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Statistics.Domain.Models;

namespace Statistics.Persistence.Ef.DataAccess.Configuration;

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