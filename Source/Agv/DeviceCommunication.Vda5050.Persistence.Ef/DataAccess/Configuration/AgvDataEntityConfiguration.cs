using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvDataEntityConfiguration : IEntityTypeConfiguration<AgvData>
{
    private const string AgvDataTableName = "DeviceCommunicationVda5050AgvData";

    /// <inheritdoc />>
    public void Configure(EntityTypeBuilder<AgvData> builder)
    {
        builder.ToTable(AgvDataTableName);
        builder.ConfigureAllProperties();

        builder.HasKey(e => e.AgvCorrelationId);
        builder.Property(e => e.Type).HasMaxLength(DbStringLimits.AgvActionTypeMaxLength);
        builder.Property(e => e.SoftwareVersion).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Model).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.IpV4Address).HasMaxLength(15);
        builder.Property(e => e.Sensors).HasJsonConversion();
    }
}