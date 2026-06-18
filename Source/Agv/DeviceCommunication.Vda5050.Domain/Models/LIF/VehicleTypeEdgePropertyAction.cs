using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Action on an edge.
/// </summary>
[PublicAPI]
public class VehicleTypeEdgePropertyAction
{
    /// <inheritdoc cref="VehicleTypeEdgePropertyAction"/>
    public VehicleTypeEdgePropertyAction(
        string actionType,
        BlockingType blockingType,
        string? actionDescription = null,
        ICollection<EdgeActionParameter>? actionParameters = null,
        RequirementType? requirementType = null)
    {
        ActionType = actionType;
        BlockingType = blockingType;
        ActionDescription = actionDescription;
        ActionParameters = actionParameters;
        RequirementType = requirementType;
    }

    /// <inheritdoc cref="VehicleTypeEdgePropertyAction"/>
    public VehicleTypeEdgePropertyAction()
    {
    }

    /// <summary>
    /// Description of the action. Optional.
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// Parameters associated with the action. Optional.
    /// </summary>
    public ICollection<EdgeActionParameter>? ActionParameters { get; set; }

    /// <summary>
    /// Type of action (e.g., move, load, unload).
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
