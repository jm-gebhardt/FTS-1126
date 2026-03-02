using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Action;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvActionEntityConfiguration : IEntityTypeConfiguration<AgvAction>
{
    private const string AgvDataTableName = "DeviceCommunicationVda5050AgvAction";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvAction> builder)
    {
        builder.ToTable(AgvDataTableName);
        builder.ConfigureAllProperties();

        builder.HasKey(e => new {e.AgvSerialNumber, e.ActionType});
        builder.Property(e => e.ActionType).HasMaxLength(DbStringLimits.AgvActionTypeMaxLength);
        builder.Property(e => e.AgvSerialNumber).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.ActionDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.ResultDescription).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.ActionParameters).HasJsonConversion();
        builder.Property(e => e.BlockingTypes).HasJsonConversion();
        builder.Property(e => e.ActionScopes).HasJsonConversion();
    }
}