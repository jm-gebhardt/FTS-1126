using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides the response carrying all agv type simulation configurations.
/// </summary>
/// <param name="Configurations">The stored agv type simulation configurations.</param>
[PublicAPI]
public record AgvTypeConfigurationsResponse(IReadOnlyCollection<SimulatedAgvTypeConfiguration> Configurations);