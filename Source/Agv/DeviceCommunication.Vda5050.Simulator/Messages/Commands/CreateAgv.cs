using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to create a simulated agv.
/// </summary>
/// <param name="Configuration">The agv configuration.</param>
/// <param name="InitialGraphNodes">Nodes of the graph. Null if no graph was available at startup.</param>
public record CreateAgv(AgvConfiguration Configuration, ICollection<GraphNode>? InitialGraphNodes = null) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId => Configuration.CorrelationId;
}