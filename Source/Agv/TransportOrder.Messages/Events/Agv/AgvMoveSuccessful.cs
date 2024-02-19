using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the response to a successful <see cref="MoveAgv"/> command.
/// </summary>
/// <param name="ActionGroupCorrelationId">The correlation id of the move action group.</param>
public record AgvMoveSuccessful(Guid ActionGroupCorrelationId);