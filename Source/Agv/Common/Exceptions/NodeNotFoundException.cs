using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// The Waypoint was not found in the graph
/// </summary>
public class NodeNotFoundException : NotFoundException
{
    /// <see cref="NodeNotFoundException"/>>
    public NodeNotFoundException(IAgv agv, string description)
        : base(description, nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
    
    /// <see cref="NodeNotFoundException"/>>
    public NodeNotFoundException(Guid agvCorrelationId, string description)
        : base(description, nameof(agvCorrelationId), agvCorrelationId)
    {
    }
}