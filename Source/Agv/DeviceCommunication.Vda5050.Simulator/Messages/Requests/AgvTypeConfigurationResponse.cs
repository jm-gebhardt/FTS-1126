using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides the response carrying a single agv type simulation configuration.
/// </summary>
/// <param name="Configuration">The agv type simulation configuration.</param>
[PublicAPI]
public record AgvTypeConfigurationResponse(SimulatedAgvTypeConfiguration Configuration);