using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the IsAgvLoaded
/// </summary>
/// <param name="IsAgvLoaded"></param>
[PublicAPI]
public record IsAgvLoadedResult(bool IsAgvLoaded);
