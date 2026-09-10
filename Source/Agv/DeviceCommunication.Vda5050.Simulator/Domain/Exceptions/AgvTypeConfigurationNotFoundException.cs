using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when no simulation configuration exists for an AGV type.
/// </summary>
public class AgvTypeConfigurationNotFoundException : NotFoundException
{
    /// <inheritdoc cref="AgvTypeConfigurationNotFoundException"/>
    public AgvTypeConfigurationNotFoundException(Guid agvTypeId)
        : base(nameof(SimulatedAgvTypeConfiguration), nameof(SimulatedAgvTypeConfiguration.Id), agvTypeId)
    {
    }
}