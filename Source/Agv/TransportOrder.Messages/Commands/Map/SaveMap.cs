namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Provides a command to save a map.
/// </summary>
/// <param name="Map">The map.</param>
public record SaveMap(Common.Domain.Models.Graphs.Map Map);