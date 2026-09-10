using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.DataAccess.Configuration;

/// <inheritdoc/>
public class SimulatedAgvTypeConfigurationEntityConfiguration : IEntityTypeConfiguration<SimulatedAgvTypeConfiguration>
{
    private const string TableName = "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SimulatedAgvTypeConfiguration> builder)
    {
        builder.ToTable(TableName);
        builder.ConfigureAllProperties(except:
        [
            nameof(SimulatedAgvTypeConfiguration.Manufacturer),
            nameof(SimulatedAgvTypeConfiguration.SeriesName)
        ]);
        builder.Ignore(e => e.Manufacturer);
        builder.Ignore(e => e.SeriesName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AgvTypeIdentifier).HasMaxLength(DbStringLimits.Description);
        builder.HasIndex(e => e.AgvTypeIdentifier).IsUnique();
    }
}