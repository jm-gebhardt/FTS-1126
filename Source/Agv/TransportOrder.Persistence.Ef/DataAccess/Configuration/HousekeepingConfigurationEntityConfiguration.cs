using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>
public class HousekeepingConfigurationEntityConfiguration : IEntityTypeConfiguration<HousekeepingConfiguration>
{
    private const string TableName = "HousekeepingConfiguration";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<HousekeepingConfiguration> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id);
        builder.Property(e => e.EntityType);
        builder.Property(e => e.OutdatedValue);
        builder.Property(e => e.OutdatedInterval);
        builder.Property(e => e.ScheduleValue);
        builder.Property(e => e.ScheduleInterval);
        builder.Property(e => e.ScheduleHour);
        builder.Property(e => e.ScheduleMinute);
    }
}
