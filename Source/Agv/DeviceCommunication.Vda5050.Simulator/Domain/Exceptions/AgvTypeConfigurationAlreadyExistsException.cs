using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when a simulation configuration for the same AGV type already exists.
/// </summary>
public class AgvTypeConfigurationAlreadyExistsException : ValidationException
{
    /// <inheritdoc cref="AgvTypeConfigurationAlreadyExistsException"/>
    public AgvTypeConfigurationAlreadyExistsException(string agvTypeIdentifier)
        : base($"A simulation configuration for the AGV type '{agvTypeIdentifier}' already exists.")
    {
    }
}