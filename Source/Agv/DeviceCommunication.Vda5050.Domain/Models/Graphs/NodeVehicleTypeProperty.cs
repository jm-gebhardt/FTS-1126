using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using BlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// Vehicle-type-specific properties for a <see cref="GraphLayoutNode"/>.
/// </summary>
[PublicAPI]
public record NodeVehicleTypeProperty
{
    /// <summary>
    /// The vehicle type this property applies to (e.g. "Manufacturer.SeriesName").
    /// </summary>
    public string VehicleTypeId { get; init; } = string.Empty;

    /// <summary>
    /// Absolute orientation of the vehicle on the node in reference to the global origin's
    /// rotation. Range: [-Pi ... Pi].
    /// </summary>
    public double? Theta { get; init; }

    /// <summary>
    /// Actions defined for this vehicle type on this node.
    /// </summary>
    public ICollection<NodeVehicleTypeAction> Actions { get; init; } = [];
}

/// <summary>
/// An action defined for a vehicle type on a node.
/// </summary>
[PublicAPI]
public record NodeVehicleTypeAction
{
    /// <summary>
    /// The action type identifier (e.g. "pick", "drop").
    /// </summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>
    /// The blocking type of the action.
    /// </summary>
    public BlockingType BlockingType { get; init; }

    /// <summary>
    /// The requirement type of the action.
    /// </summary>
    public RequirementType? RequirementType { get; init; }

    /// <summary>
    /// The action parameters.
    /// </summary>
    public ICollection<OrderActionParameter> Parameters { get; init; } = [];
}