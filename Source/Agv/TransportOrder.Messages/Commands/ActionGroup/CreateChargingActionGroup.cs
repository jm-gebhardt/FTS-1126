using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a charging <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="Agv"></param>
public record CreateChargingActionGroup(IAgv Agv);