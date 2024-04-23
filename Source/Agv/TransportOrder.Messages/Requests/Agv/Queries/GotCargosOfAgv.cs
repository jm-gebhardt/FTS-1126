using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the Cargos of an Agv
/// </summary>
/// <param name="Cargos"></param>
[PublicAPI]
public record GotCargosOfAgv(ICollection<Cargo> Cargos);