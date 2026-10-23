using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Vda5050Simulator.DataAccess;

public class SimulatedAgvTypeConfigurationEntityConfiguration : IEntityTypeConfiguration<SimulatedAgvTypeConfiguration>
{
    private const string TableName = "DeviceCommunicationVda5050SimulatorAgvTypeConfiguration";

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SimulatedAgvTypeConfiguration> builder)
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