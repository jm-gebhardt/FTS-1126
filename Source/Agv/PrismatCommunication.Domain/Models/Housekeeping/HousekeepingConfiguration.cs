using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;

/// <summary>
/// Provides a configuration for housekeeping of a specific entity type.
/// </summary>
[PublicAPI]
public class HousekeepingConfiguration
{
    /// <summary>The id of the housekeeping configuration.</summary>
    public Guid Id { get; set; }

    /// <summary>The entity type this configuration applies to.</summary>
    public HousekeepingEntityType EntityType { get; set; }

    /// <summary>The value that applies to the <see cref="OutdatedInterval"/> for entries becoming outdated.</summary>
    public int OutdatedValue { get; set; }

    /// <summary>The interval that applies for outdated entries.</summary>
    public HousekeepingInterval OutdatedInterval { get; set; }

    /// <summary>How many <see cref="ScheduleInterval"/> units between each housekeeping run.</summary>
    public int ScheduleValue { get; set; }

    /// <summary>The time unit for the housekeeping schedule.</summary>
    public HousekeepingInterval ScheduleInterval { get; set; }

    /// <summary>The hour of day (0–23) at which the job runs. Only used when <see cref="ScheduleInterval"/> is Days or Weeks.</summary>
    public int ScheduleHour { get; set; }

    /// <summary>The minute (0–59) at which the job runs. Only used when <see cref="ScheduleInterval"/> is Days or Weeks.</summary>
    public int ScheduleMinute { get; set; }

    private const string ConfigurationSection = "HOUSEKEEPING";

    /// <inheritdoc cref="HousekeepingConfiguration"/>
    public HousekeepingConfiguration() => Id = Guid.NewGuid();

    /// <inheritdoc cref="HousekeepingConfiguration"/>
    public HousekeepingConfiguration(Guid id, HousekeepingEntityType entityType, int outdatedValue, HousekeepingInterval outdatedInterval,
        int scheduleValue, HousekeepingInterval scheduleInterval, int scheduleHour, int scheduleMinute)
    {
        Id = id;
        EntityType = entityType;
        OutdatedValue = outdatedValue;
        OutdatedInterval = outdatedInterval;
        ScheduleValue = scheduleValue;
        ScheduleInterval = scheduleInterval;
        ScheduleHour = scheduleHour;
        ScheduleMinute = scheduleMinute;
    }

    /// <summary>
    /// Creates a default configuration for the given entity type, with values optionally overridden from
    /// <c>HOUSEKEEPING:{ENTITY_TYPE}:*</c> environment variables.
    /// </summary>
    public static HousekeepingConfiguration CreateDefault(HousekeepingEntityType entityType, IConfiguration configuration)
    {
        var section = configuration.GetSection($"{ConfigurationSection}:{entityType.ToString().ToUpperInvariant()}");

        var (defaultOutdated, defaultScheduleHour) = entityType switch
        {
            HousekeepingEntityType.Orders   => (90, 4),
            _                             => (14, 1)
        };

        return new HousekeepingConfiguration
        {
            Id               = Guid.NewGuid(),
            EntityType       = entityType,
            OutdatedValue    = section.GetValue("OUTDATED_VALUE", defaultOutdated),
            OutdatedInterval = section.GetValue("OUTDATED_INTERVAL", HousekeepingInterval.Days),
            ScheduleValue    = section.GetValue("SCHEDULE_VALUE", 1),
            ScheduleInterval = section.GetValue("SCHEDULE_INTERVAL", HousekeepingInterval.Days),
            ScheduleHour     = section.GetValue("SCHEDULE_HOUR", defaultScheduleHour),
            ScheduleMinute   = section.GetValue("SCHEDULE_MINUTE", 0)
        };
    }

    /// <summary>
    /// Returns the cut-off time: entries older than this should be deleted.
    /// </summary>
    public DateTime GetOutdatedTime()
    {
        var now = DateTime.UtcNow;
        return OutdatedInterval switch
        {
            HousekeepingInterval.Hours  => now.AddHours(-OutdatedValue),
            HousekeepingInterval.Days   => now.AddDays(-OutdatedValue),
            HousekeepingInterval.Weeks  => now.AddDays(-7 * OutdatedValue),
            HousekeepingInterval.Months => now.AddMonths(-OutdatedValue),
            HousekeepingInterval.Years  => now.AddYears(-OutdatedValue),
            _                           => now.AddDays(-14)
        };
    }

    /// <summary>
    /// Builds the Quartz <see cref="IScheduleBuilder"/> for this configuration.
    /// </summary>
    public IScheduleBuilder BuildSchedule()
    {
        return ScheduleInterval switch
        {
            HousekeepingInterval.Hours =>
                SimpleScheduleBuilder.Create()
                    .WithIntervalInHours(ScheduleValue)
                    .RepeatForever(),

            HousekeepingInterval.Weeks =>
                CronScheduleBuilder.CronSchedule(
                    $"0 {ScheduleMinute} {ScheduleHour} ? * MON/{ScheduleValue * 7}"),

            // Days and everything else: run daily (every N days) at the configured hour/minute
            _ =>
                CronScheduleBuilder.CronSchedule(
                    $"0 {ScheduleMinute} {ScheduleHour} 1/{ScheduleValue} * ?")
        };
    }
}
