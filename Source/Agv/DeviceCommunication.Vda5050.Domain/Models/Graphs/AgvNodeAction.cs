using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using BlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// An action an agv can run on a node.
/// </summary>
[PublicAPI]
public record AgvNodeAction
{
    /// <summary>
    /// The action type.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    /// <returns></returns>
    public Guid AgvCorrelationId { get; set; }

    /// <summary>
    /// The blocking type of the action.
    /// </summary>
    public BlockingType BlockingType { get; set; }

    /// <summary>
    /// The id of the node.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Absolute orientation of the vehicle on the node in reference to the global origin’s
    ///  rotation. Range: [-Pi ... Pi]
    /// </summary>
    public double? Theta { get; set; }

    /// <summary>
    /// The requirement type of the action.
    /// </summary>
    public RequirementType? RequirementType { get; set; }

    /// <summary>
    /// The action parameters.
    /// </summary>
    public ICollection<OrderActionParameter> Parameters { get; set; } = [];
}

/// <summary>
/// Provides a requirement type for actions.
/// </summary>
public enum RequirementType
{
    /// <summary>
    /// The (third-party) master control system must always communicate this action.
    /// </summary>
    Conditional,

    /// <summary>
    /// The action may or may not be communicated at the master control system's discretion.
    /// </summary>
    Optional,

    /// <summary>
    /// The action is required.
    /// </summary>
    Required
}