using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the hasAgvCargo
/// </summary>
/// <param name="HasAgvCargo"></param>
[PublicAPI]
public record HasAgvCargoResult(bool HasAgvCargo);
