using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Provides a dto for action groups.
/// </summary>
[PublicAPI]
public record ActionGroupDto : DatedMessage
{
    private IList<AgvActionDto> _actions = new List<AgvActionDto>();

    /// <summary>
    /// The correlationId of action group.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The order id.
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// The assigned agv.
    /// </summary>
    public Guid? AssignedAgv { get; init; }

    /// <summary>
    /// The creation date.
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// The date when the action group is started.
    /// </summary>
    public DateTime? StartedDate { get; init; }

    /// <summary>
    /// The date when the action group is completed.
    /// </summary>
    public DateTime? CompletedDate { get; init; }

    /// <summary>
    /// The state of the action group. 
    /// </summary>
    public ActionGroupState State { get; init; }

    /// <summary>
    /// The creator of the action group.
    /// </summary>
    public ActionGroupCreator Creator { get; init; }

    /// <summary>
    /// List of actions
    /// </summary>
    public IList<AgvActionDto> Actions
    {
        get => _actions;
        init
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            // can be null because of AutoMapper
            _actions = value?.OrderBy(a => a.Sequence).ToList() ?? new List<AgvActionDto>();
        }
    }

    /// <summary>
    /// Gets if the action group was created manually ( i.e. not by the system).
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public ActionGroupDto()
    {
        OrderId = null!;
        Actions = null!;
    }
}