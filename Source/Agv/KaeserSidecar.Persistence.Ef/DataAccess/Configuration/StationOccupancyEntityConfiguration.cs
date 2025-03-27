using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class StationOccupancyEntityConfiguration : IEntityTypeConfiguration<StationOccupancy>
{
    private const string TableName = "StationOccupancy";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StationOccupancy> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Station);
        builder.Property(e => e.Count);
        builder.Property(e => e.Station).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.AgvCorrelationId);
    }
}