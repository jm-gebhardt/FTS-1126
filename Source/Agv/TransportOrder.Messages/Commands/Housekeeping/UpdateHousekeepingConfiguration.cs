using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;

/// <summary>
/// Provides a command to update the housekeeping configuration.
/// </summary>
/// <param name="OutdatedValue">The value for the outdated time.</param>
/// <param name="OutdatedInterval">The interval for the outdated time.</param>
public record UpdateHousekeepingConfiguration(int OutdatedValue, HousekeepingInterval OutdatedInterval);