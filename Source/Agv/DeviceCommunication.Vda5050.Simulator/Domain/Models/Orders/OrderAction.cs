using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// Action that the AGV is to perform on a node or edge.
/// </summary>
[PublicAPI]
public record OrderAction
{
    /// <summary>
    /// Name of the action as described in "Actions and Parameters". Identifies the function of the action.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Unique ID to identify the action and map it to the actionState in the state.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Regulates if the action is allowed to be executed during movement and/or parallel to other actions.
    /// </summary>
    public BlockingType BlockingType { get; set; }

    /// <summary>
    /// Array of action parameters for the indicated action.
    /// Optional: omit if the action has no parameters.
    /// </summary>
    public ICollection<OrderActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// Started time of the action;
    /// </summary>
    public DateTime? StartedTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromSeconds(10);

    /// <inheritdoc cref="OrderAction"/>
    public OrderAction(string actionType, string? actionDescription, BlockingType blockingType, ICollection<OrderActionParameter>? actionParameters)
    {
        ActionType = actionType;
        ActionDescription = actionDescription;
        BlockingType = blockingType;
        ActionParameters = actionParameters;
        ActionId = Guid.NewGuid().ToString();
    }
}