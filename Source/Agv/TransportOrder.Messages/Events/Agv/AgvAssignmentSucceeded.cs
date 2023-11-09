using Gebhardt.StoreWare.Agv.Common.Contracts;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event that provides a assigned AGV for an order 
/// </summary>
[PublicAPI]
public record AgvAssignmentSucceeded : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc />
    public string AgvId { get; set; }

    /// <summary>
    /// The AGV correlation id 
    /// </summary>
    public Guid AgvCorrelationId { get; set; }

    /// <summary>
    /// List of node ids that define the route
    /// </summary>
    public string[] Nodes { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AgvAssignmentSucceeded"/>
    /// </summary>
    public AgvAssignmentSucceeded(Guid correlationId, string agvId, Guid agvCorrelationId, string[] nodes)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        AgvCorrelationId = agvCorrelationId;
        Nodes = nodes;
    }
}
