using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Provides a command to save a map for an agv.
/// </summary>
/// <param name="Map">The map.</param>
/// <param name="AgvId">The agv id.</param>
public record SaveMap(AgvMap Map, Guid AgvId);