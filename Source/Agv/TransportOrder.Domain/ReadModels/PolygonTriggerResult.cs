using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Represents a read-model for polygon trigger evaluation.
/// </summary>
[PublicAPI]
public class PolygonTriggerResult
{
    /// <summary>
    /// The trigger location.
    /// </summary>
    public Location Location { get; init; } = null!;

    /// <summary>
    /// The action the agv did on the trigger.
    /// </summary>
    public PolygonTriggerAction Action { get; init; }
}

/// <summary>
/// Polygon trigger action.
/// </summary>
public enum PolygonTriggerAction
{
    /// <summary>
    /// An agv entered the polygon trigger.
    /// </summary>
    Entered,
    
    /// <summary>
    /// An agv left the polygon trigger.
    /// </summary>
    Left
}
