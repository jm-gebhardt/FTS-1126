using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <summary>
/// Data transfer object for a filtered action group.
/// </summary>
[PublicAPI]
public record FilteredActionGroupDto
{
    /// <inheritdoc cref="ActionGroup.CorrelationId"/>
    [SwaggerFilterableProperty]
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="ActionGroup.OrderId"/>
    [SwaggerFilterableProperty]
    public string OrderId { get; init; } = null!;

    /// <inheritdoc cref="ActionGroup.AssignedAgv"/>
    public FilteredAgvDto? AssignedAgv { get; init; }

    /// <inheritdoc cref="ActionGroup.CreatedDate"/>
    [SwaggerFilterableProperty]
    public DateTime CreatedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.StartedDate"/>
    [SwaggerFilterableProperty]
    public DateTime? StartedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.CompletedDate"/>
    [SwaggerFilterableProperty]
    public DateTime? CompletedDate { get; init; }

    /// <inheritdoc cref="ActionGroup.State"/>
    [SwaggerFilterableProperty]
    public ActionGroupState State { get; init; }

    /// <inheritdoc cref="ActionGroup.IsQueued"/>
    [SwaggerFilterableProperty]
    public bool IsQueued { get; init; }

    /// <inheritdoc cref="ActionGroup.Actions"/>
    public List<FilteredActionDto>? Actions { get; init; }

    /// <inheritdoc cref="ActionGroup.IsManual"/>
    [SwaggerFilterableProperty]
    public bool IsManual { get; init; }

    /// <inheritdoc cref="ActionGroup.Creator"/>
    [SwaggerFilterableProperty]
    public ActionGroupCreator Creator { get; init; }

    /// <inheritdoc cref="ActionGroup.EstimatedDuration"/>
    [SwaggerFilterableProperty]
    public TimeSpan? EstimatedDuration { get; init; }

    /// <inheritdoc cref="ActionGroup.HasPriority"/>
    [SwaggerFilterableProperty]
    public bool HasPriority { get; init; }

    /// <inheritdoc cref="ActionGroup.HandlingUnit"/>
    [SwaggerFilterableProperty]
    public string? HandlingUnit { get; init; }
}