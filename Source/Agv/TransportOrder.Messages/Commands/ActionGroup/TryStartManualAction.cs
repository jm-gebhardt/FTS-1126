using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start a manual <see cref="Action"/> for an agv.
/// </summary>
/// <param name="Agv">The agv.</param>
public record TryStartManualAction(IAgv Agv);