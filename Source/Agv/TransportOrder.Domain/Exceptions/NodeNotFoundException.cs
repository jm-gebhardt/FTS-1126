using Gebhardt.StoreWare.Agv.Common.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// The Waypoint was not found in the graph
/// </summary>
public class NodeNotFoundException : NotFoundException
{
    /// <see cref="NodeNotFoundException"/>>
    [PublicAPI]
    public NodeNotFoundException(Models.Agv.Agv agv, string description)
        : base(description, nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
    
    /// <see cref="NodeNotFoundException"/>>
    public NodeNotFoundException(Guid agvCorrelationId, string description)
        : base(description, nameof(agvCorrelationId), agvCorrelationId)
    {
    }
}