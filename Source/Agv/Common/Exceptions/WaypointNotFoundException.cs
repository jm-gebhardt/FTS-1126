using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// The Waypoint was not found in the graph
/// </summary>
public class WaypointNotFoundException : NotFoundException
{
    /// <see cref="AgvGraphNotDefinedException"/>>
    public WaypointNotFoundException(IAgv agv, string description)
        : base(description, nameof(agv.CorrelationId), agv.CorrelationId)
    {
    }
}