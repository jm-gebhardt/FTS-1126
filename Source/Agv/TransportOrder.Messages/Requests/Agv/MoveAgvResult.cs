using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a successful result for <see cref="MoveAgv"/> command
/// </summary>
public record MoveAgvResult(Guid ActionGroupCorrelationId);