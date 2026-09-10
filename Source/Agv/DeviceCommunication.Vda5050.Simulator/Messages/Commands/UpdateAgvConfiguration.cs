using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to apply a new configuration to a running simulated agv.
/// </summary>
/// <param name="Configuration">The agv configuration.</param>
public record UpdateAgvConfiguration(AgvConfiguration Configuration) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId => Configuration.CorrelationId;
}