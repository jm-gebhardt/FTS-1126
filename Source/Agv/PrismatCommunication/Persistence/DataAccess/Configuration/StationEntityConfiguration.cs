using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc cref="IEntityTypeConfiguration{T}"/>>
public class StationEntityConfiguration:IEntityTypeConfiguration<Station>
{
    private const string TableName = "PrismatCommunicationStations";

    /// <inheritdoc cref="IEntityTypeConfiguration{T}"/>>
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.NodeId);
        builder.Property(e => e.NodeName).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.Source).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.Target).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.GroupId);
        builder.Property(e => e.AgvCorrelationId);
        builder.Property(e => e.Occupancy);
    }
}