using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using JetBrains.Annotations;

namespace TransportOrder.Domain.ReadModels;

/// <summary>
/// Represents a read model for agv action groups.
/// </summary>
/// <param name="CorrelationId">The correlation of the action group.</param>
/// <param name="OrderId">The order id.</param>
/// <param name="AssignedAgv">The agv assigned to the action group.</param>
/// <param name="CreatedDate">The creation date.</param>
/// <param name="StartedDate">The start date.</param>
/// <param name="CompletedDate">The completion date.</param>
/// <param name="State">The state of the action group.</param>
/// <param name="Creator">The creator of the action group.</param>
/// <param name="Actions">The action.</param>
/// <param name="IsManual">The manual creation mode flag.</param>
public record ActionGroupReadModel(Guid CorrelationId, string OrderId, Guid? AssignedAgv,
    DateTime CreatedDate, DateTime? StartedDate, DateTime? CompletedDate, ActionGroupState State,
    ActionGroupCreator Creator, ICollection<AgvActionReadModel> Actions, [UsedImplicitly] bool IsManual);