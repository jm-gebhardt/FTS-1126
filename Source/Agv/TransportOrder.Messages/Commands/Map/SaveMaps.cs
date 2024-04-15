namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Command to save multiple maps.
/// </summary>
/// <param name="Maps">The maps.</param>
public record SaveMaps(ICollection<Common.Domain.Models.Graphs.Map> Maps);