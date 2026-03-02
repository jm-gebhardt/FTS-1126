using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Action;

/// <summary>
/// An action type supported by the AGV, including its parameters and applicable scopes.
/// </summary>
[PublicAPI]
public record AgvAction
{
    /// <summary>
    /// The serial number of the agv.
    /// </summary>
    public string AgvSerialNumber { get; set; } = null!;

    /// <summary>
    /// Unique actionType corresponding to action.actionType.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Free text description of the action.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// List of allowed scopes for using this action type.
    /// </summary>
    public ICollection<ActionScope> ActionScopes { get; set; } = [];

    /// <summary>
    /// List of parameters for this action. Omit or leave empty if the action has no parameters.
    /// </summary>
    public ICollection<AgvActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// Free text description of the result description.
    /// </summary>
    public string? ResultDescription { get; set; }

    /// <summary>
    /// Array of possible blocking types for this action.
    /// </summary>
    public ICollection<BlockingType>? BlockingTypes { get; set; }
}