using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event that provides a assigned AGV for an order 
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AgvAssignmentSucceeded : CorrelatedBy<Guid>, IAgvId
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public string AgvId { get; }

    /// <summary>
    /// The AGV correlation id 
    /// </summary>
    public Guid AgvCorrelationId { get; }
        
    /// <summary>
    /// List of node ids that define the route
    /// </summary>
    public string[] Nodes { get; }

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