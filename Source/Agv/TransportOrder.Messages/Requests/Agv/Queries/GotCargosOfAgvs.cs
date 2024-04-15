using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the Cargos for multiple Agvs
/// </summary>
/// <param name="CargosByAgvCorrelationId"></param>
[PublicAPI]
public record GotCargosOfAgvs(IReadOnlyDictionary<Guid, ICollection<Cargo>> CargosByAgvCorrelationId);