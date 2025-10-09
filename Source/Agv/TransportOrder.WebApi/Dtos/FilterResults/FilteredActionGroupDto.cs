using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered action group.
/// </summary>
[PublicAPI]
public record FilteredActionGroupDto
{
    /// <summary>
    /// Correlation id of this action group.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// A unique ID of the order that clamps all the corresponding actions.
    /// </summary>
    public string OrderId { get; init; } = null!;

    /// <summary>
    /// The id of the assigned agv that will execute the ActionGroup.
    /// </summary>
    public FilteredAgvDto? AssignedAgv { get; init; }

    /// <summary>
    /// The creation date of the action (UTC).
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// The date the action has been started (UTC).
    /// </summary>
    public DateTime? StartedDate { get; init; }

    /// <summary>
    /// The date the action has been completed (UTC).
    /// </summary>
    public DateTime? CompletedDate { get; init; }

    /// <summary>
    /// The state of the ActionGroup.
    /// </summary>
    public ActionGroupState State { get; init; }

    /// <summary>
    /// True, if the action group is queued.
    /// </summary>
    public bool IsQueued { get; init; }

    /// <summary>
    /// List of actions (ordered by sequence).
    /// </summary>
    public List<FilteredActionDto>? Actions { get; init; }

    /// <summary>
    /// If true, then this ActionGroup was created manually, not by the system.
    /// </summary>
    public bool IsManual { get; init; }

    /// <summary>
    /// The creator of the <see cref="ActionGroup"/>.
    /// </summary>
    public ActionGroupCreator Creator { get; init; }

    /// <summary>
    /// The estimated duration based on the estimated duration of the actions.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; init; }

    /// <summary>
    /// If true, then this ActionGroup has priority over others (starts immediately).
    /// </summary>
    public bool HasPriority { get; init; }

    /// <summary>
    /// The handling unit corresponding to this ActionGroup.
    /// </summary>
    public string? HandlingUnit { get; init; }

    /// <summary>
    /// Automapper ctor.
    /// </summary>
    public FilteredActionGroupDto()
    {
    }
}