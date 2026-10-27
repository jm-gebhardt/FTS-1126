using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.Vda5050Simulator.Domain;

/// <summary>
/// Raised when no simulation configuration exists for an AGV type.
/// </summary>
public class AgvTypeConfigurationNotFoundException(Guid agvTypeId)
    : NotFoundException(nameof(AgvTypeConfiguration), nameof(AgvTypeConfiguration.Id), agvTypeId);

/// <summary>
/// Raised when a simulation configuration for the same AGV type already exists.
/// </summary>
public class AgvTypeConfigurationAlreadyExistsException(string agvTypeIdentifier)
    : ValidationException($"A simulation configuration for the AGV type '{agvTypeIdentifier}' already exists.");
