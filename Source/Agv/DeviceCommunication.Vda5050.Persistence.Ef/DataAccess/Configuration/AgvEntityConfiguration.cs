using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class AgvEntityConfiguration : IEntityTypeConfiguration<Agv>
{
    private const string TableName = "DeviceCommunicationVda5050Agv";
    
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Agv> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.CorrelationId);
        builder.Property(e => e.Manufacturer).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.SeriesName).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.SerialNumber).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.CurrentOrder);
        builder.Property(e => e.CurrentFleetManagerActionId);
        builder.Ignore(e => e.VehicleTypeId);
    }
}