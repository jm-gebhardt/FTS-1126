using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using JetBrains.Annotations;
using BlockingType = Gebhardt.Acc.Common.Domain.Models.BlockingType;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// An action an agv can run on a node.
/// </summary>
[PublicAPI]
public record AgvEdgeAction
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
    /// The id of the edge.
    /// </summary>
    public Guid EdgeId { get; set; }

    /// <summary>
    /// The requirement type of the action.
    /// </summary>
    public RequirementType? RequirementType { get; set; }

    /// <summary>
    /// The action parameters.
    /// </summary>
    public ICollection<OrderActionParameter> Parameters { get; set; } = [];
}