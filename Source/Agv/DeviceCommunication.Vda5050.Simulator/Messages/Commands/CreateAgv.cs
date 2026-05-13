using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to create a simulated agv.
/// </summary>
/// <param name="Configuration">The agv configuration.</param>
public record CreateAgv(AgvConfiguration Configuration) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId => Configuration.CorrelationId;
}