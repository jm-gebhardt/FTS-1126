using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;

/// <summary>
/// Action Group model
/// </summary>
[PublicAPI]
public record CoreActionGroupDto
{
    private IList<CoreAgvActionDto> _actions = new List<CoreAgvActionDto>();

    /// <summary>
    /// Gets the correlationId of action group.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the order id.
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// Gets the assigned agv.
    /// </summary>
    public Guid? AssignedAgv { get; init; }

    /// <summary>
    /// Gets the creation date.
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// Gets the date when the action group is started.
    /// </summary>
    public DateTime? StartedDate { get; init; }

    /// <summary>
    /// Gets the date when the action group is completed.
    /// </summary>
    public DateTime? CompletedDate { get; init; }

    /// <summary>
    /// Gets the state of the action group. 
    /// </summary>
    public ActionGroupState State { get; init; }

    /// <summary>
    /// The creator of the action group.
    /// </summary>
    public ActionGroupCreator Creator { get; init; }

    /// <summary>
    /// List of actions
    /// </summary>
    public IList<CoreAgvActionDto> Actions
    {
        get => _actions;
        init
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            // can be null because of AutoMapper
            _actions = value?.OrderBy(a => a.Sequence).ToList() ?? new List<CoreAgvActionDto>();
        }
    }

    /// <summary>
    /// Gets if the action group was created manually ( i.e. not by the system).
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// AutoMapper ctor
    /// </summary>
    public CoreActionGroupDto()
    {
        OrderId = null!;
        Actions = null!;
    }
}