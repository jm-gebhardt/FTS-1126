using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the Cargos of an Agv
/// </summary>
/// <param name="Cargos"></param>
[PublicAPI]
public record GotCargosOfAgv(ICollection<Cargo> Cargos);