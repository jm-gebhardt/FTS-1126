namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event triggered when a new <see cref="ActionGroup"/> was created for an agv.
/// </summary>
/// <param name="ActionGroup">The action group.</param>
/// <param name="AssignedAgv">The assigned agv.</param>
public record ActionGroupCreatedForAgv(Common.Messages.ActionGroups.ActionGroup ActionGroup, Guid AssignedAgv);