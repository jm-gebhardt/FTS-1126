using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// An action defined for a vehicle type on an edge.
/// </summary>
[PublicAPI]
public record EdgeVehicleTypeAction
{
    /// <summary>
    /// The action type identifier.
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