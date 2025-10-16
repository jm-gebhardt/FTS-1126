using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc />>
public class ShortLoopTargetStationEntityConfiguration: IEntityTypeConfiguration<ShortLoopWeTargetStation>
{
    private const string TableName = "PrismatCommunicationShortLoopTargetStation";
   
    /// <inheritdoc />>
    public void Configure(EntityTypeBuilder<ShortLoopWeTargetStation> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Current).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.Primary).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.LockedStations).HasJsonConversion();
    }
}