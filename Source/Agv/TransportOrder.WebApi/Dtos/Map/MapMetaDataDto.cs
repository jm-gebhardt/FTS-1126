using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// Provides a dto fro map meta data.
/// </summary>
/// <param name="CorrelationId">Map Correlation Id</param>
/// <param name="MapId">Map Id</param>
[UsedImplicitly]
public record MapMetaDataDto(Guid CorrelationId, string MapId);