namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents an event that is raised when an agv has no other actions to perform.
/// </summary>
/// <param name="Agv">The agv.</param>
public record AgvCanStartIdleAction(Domain.Models.Agv.Agv Agv);