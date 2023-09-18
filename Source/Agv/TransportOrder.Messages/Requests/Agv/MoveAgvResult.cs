using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a successful result for <see cref="MoveAgv"/> command
/// </summary>
public record MoveAgvResult(Guid ActionGroupCorrelationId);