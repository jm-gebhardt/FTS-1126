using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start the next <see cref="Action"/>
/// inside the current <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="Agv">The agv.</param>
public record TryStartNextActionInActionGroup(IAgv Agv);