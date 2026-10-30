using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// Agv Action
/// </summary>
[PublicAPI]
public record AgvAction : CorrelatedBy<Guid>, IParentedBy<Guid>, ISaga
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc/>
    public Guid ParentId { get; init; }

    /// <inheritdoc/>
    public int Sequence { get; init; }

    /// <summary>
    /// The current state of the action
    /// </summary>
    public ActionState State { get; set; } = ActionState.Created;

    /// <summary>
    /// The type of the action, e.g. Pick or Drop
    /// </summary>
    public ActionType ActionType { get; init; }

    /// <summary>
    /// The target of the action.
    /// </summary>
    public ActionTarget Target { get; init; }

    /// <summary>
    /// True, if the action is heading towards a position given
    /// by coordinates instead of node id.
    /// </summary>
    public bool IsMoveToCoordinate => Target.Node?.Id == null && Target.Position != null;

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public ICollection<GraphNode> Nodes { get; set; } = [];

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public ICollection<Guid> Edges { get; set; } = [];

    /// <summary>
    /// Date the action was created (UTC)
    /// </summary>
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date the action has been started (UTC)
    /// </summary>
    public DateTime? StartedDate { get; set; }

    /// <summary>
    /// Date the action has been completed (UTC)
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Date the action has been paused (UTC)
    /// </summary>
    public DateTime? PausedDate { get; set; }

    /// <summary>
    /// Duration (in seconds) that the action was run previously and paused for 
    /// </summary>
    public long PriorRunAndPauseDuration { get; set; }

    /// <summary>
    /// Indicates whether the action group is marked as skipped.
    /// </summary>
    public bool IsSkipped { get; set; }

    /// <summary>
    /// Incremented each time the action is updated on the agv while it runs.
    /// </summary>
    public int Revision { get; init; }

    /// <summary>
    /// Information that will be sent to the agv.
    /// </summary>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; set; }

    /// <summary>
    /// Additional information for this action, often used for external systems.
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// True, if the action is registered for a Doppelspiel
    /// </summary>
    public bool IsRegisteredForDoppelspiel { get; set; }

    /// <summary>
    /// The correlation id of the Doppelspiel partner order, if any.
    /// </summary>
    public Guid? DoppelspielPartner { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNode">Target node of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    /// <param name="infoToAgv">Information that will be sent to the agv.</param>
    public AgvAction(Guid correlationId, ActionType actionType, GraphNode? targetNode, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = new ActionTarget(targetNode);
        ParentId = parentId;
        InfoToAgv = infoToAgv;
    }

    /// <inheritdoc cref="AgvAction"/>>
    public AgvAction(Guid correlationId, ActionType actionType, ActionTarget target, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = target;
        ParentId = parentId;
        InfoToAgv = infoToAgv;
    }

    /// <summary>
    /// Just to satisfy Entity Framework
    /// </summary>
    [UsedImplicitly]
    [JsonConstructor]
    public AgvAction()
    {
        CorrelationId = Guid.Empty;
        ActionType = ActionType.Pick;
        Target = new ActionTarget(null);
        ParentId = Guid.Empty;
    }
}

/// <summary>
/// Keys for extra information that will be sent to the agv.
/// </summary>
public enum InfoToAgvKey
{
    /// <summary>
    /// Information about cargo rotation.
    /// </summary>
    CargoRotation,

    /// <summary>
    /// Information about the station docking direction.
    /// </summary>
    DockFront,

    /// <summary>
    /// The lift height in [m] at the station for action.
    /// </summary>
    LiftHeight,

    /// <summary>
    /// Force dropping direction to be backwards
    /// </summary>
    ForceDroppingBackwards
}