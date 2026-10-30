using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Action on a node.
/// </summary>
[PublicAPI]
public class VehicleTypeNodePropertyAction
{
    /// <inheritdoc cref="VehicleTypeNodePropertyAction"/>
    public VehicleTypeNodePropertyAction(
        string actionType,
        BlockingType blockingType,
        string? actionDescription = null,
        ICollection<NodeActionParameter>? actionParameters = null,
        RequirementType? requirementType = null)
    {
        ActionType = actionType;
        BlockingType = blockingType;
        ActionDescription = actionDescription;
        ActionParameters = actionParameters;
        RequirementType = requirementType;
    }

    /// <inheritdoc cref="VehicleTypeNodePropertyAction"/>
    public VehicleTypeNodePropertyAction()
    {
    }

    /// <summary>
    /// Description of the action. Optional.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Parameters associated with the action. Optional.
    /// </summary>
    public ICollection<NodeActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// Type of action as defined by VDA 5050 (e.g., pick, drop, startCharging) or a vehicle-specific custom action.
    /// Only pick, drop and startCharging are currently mapped to routing actions; other values are ignored.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Specifies if the action is blocking. NONE: allows moving and other actions. SOFT: allows other actions, but not moving. HARD: is the only allowed action at this time.
    /// </summary>
    public BlockingType BlockingType { get; set; } = BlockingType.NONE;

    /// <summary>
    /// Defines if the action is required. REQUIRED: must always be communicated. CONDITIONAL: may or may not be required. OPTIONAL: may or may not be communicated at the master control system's discretion.
    /// </summary>
    public RequirementType? RequirementType { get; set; }
}
