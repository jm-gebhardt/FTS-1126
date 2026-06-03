using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;

/// <summary>
/// Command to update the housekeeping configuration for a specific entity type.
/// </summary>
/// <param name="EntityType">The entity type to configure.</param>
/// <param name="OutdatedValue">The value for the outdated time.</param>
/// <param name="OutdatedInterval">The interval for the outdated time.</param>
/// <param name="ScheduleValue">How many schedule interval units between runs.</param>
/// <param name="ScheduleInterval">The time unit for the schedule.</param>
/// <param name="ScheduleHour">Hour of day (0–23) at which the job runs.</param>
/// <param name="ScheduleMinute">Minute (0–59) at which the job runs.</param>
public record UpdateHousekeepingConfiguration(
    HousekeepingEntityType EntityType,
    int OutdatedValue,
    HousekeepingInterval OutdatedInterval,
    int ScheduleValue,
    HousekeepingInterval ScheduleInterval,
    int ScheduleHour,
    int ScheduleMinute);
