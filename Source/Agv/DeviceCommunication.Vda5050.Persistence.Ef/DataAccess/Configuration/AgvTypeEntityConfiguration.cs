using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvTypeEntityConfiguration : IEntityTypeConfiguration<AgvType>
{
    private const string AgvTypeTableName = "DeviceCommunicationVda5050AgvType";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvType> builder)
    {
        builder.ToTable(AgvTypeTableName);
        builder.ConfigureAllProperties();

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AgvTypeIdentifier).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Manufacturer).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.SeriesName).HasMaxLength(DbStringLimits.Description);
        builder.HasIndex(e => e.AgvTypeIdentifier).IsUnique();
    }
}