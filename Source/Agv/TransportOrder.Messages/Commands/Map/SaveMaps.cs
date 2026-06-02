namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Map;

/// <summary>
/// Command to save multiple maps.
/// </summary>
/// <param name="Maps">The maps.</param>
/// <param name="AgvCorrelationId">The correlationId of the AGV which provides the map.</param>
public record SaveMaps(ICollection<Common.Domain.Models.Graphs.Map> Maps, Guid AgvCorrelationId);