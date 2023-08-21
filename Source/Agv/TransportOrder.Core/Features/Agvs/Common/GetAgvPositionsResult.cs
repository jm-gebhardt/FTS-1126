using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for the GetAgvPositions request
/// </summary>
public record GetAgvPositionsResult(List<AgvPositionStatusDto>? AgvPositions);