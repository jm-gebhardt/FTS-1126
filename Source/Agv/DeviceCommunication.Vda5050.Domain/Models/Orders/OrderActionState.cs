using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// State of an action that is currently being executed or waiting for execution.
/// </summary>
[PublicAPI]
public record OrderActionState
{
    /// <summary>
    /// Unique actionId.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// actionType of the action.
    /// Optional: only for informational or visualization purposes.
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Additional information on the current action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Current status of the action.
    /// </summary>
    public VehicleActionStatus ActionStatus { get; set; }

    /// <summary>
    /// Description of the result, e.g., the result of a RFID-read.
    /// Errors will be transmitted in errors.
    /// </summary>
    public string? ResultDescription { get; set; }
}
