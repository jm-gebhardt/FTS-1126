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
        // TODO: Add relation to Station
        builder.HasKey(e => e.StationId);
        builder.Property(e => e.Count);
        // TODO: remove HasMaxLength as not needed for Guid type
        builder.Property(e => e.StationId).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.AgvCorrelationId);
    }
}