using System.Text.Json.Serialization;
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
    public bool IsMoveToCoordinate => Target.NodeId == null && Target.Position != null;

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public ICollection<string> Nodes { get; set; } = [];

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
    /// Time that the action was paused for
    /// </summary>
    public TimeSpan PausedDuration { get; set; }

    /// <summary>
    /// Information that will be sent to the agv.
    /// </summary>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNodeId">Target node id of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    /// <param name="infoToAgv">Information that will be sent to the agv.</param>
    public AgvAction(Guid correlationId, ActionType actionType, string targetNodeId, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = new ActionTarget(targetNodeId);
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
        Target = new ActionTarget(string.Empty);
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
    LiftHeight
}