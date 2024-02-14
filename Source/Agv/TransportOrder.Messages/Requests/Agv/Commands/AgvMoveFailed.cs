using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;

/// <summary>
/// Represents the response to a failed <see cref="MoveAgv"/> command.
/// </summary>
public record AgvMoveFailed(string Reason);