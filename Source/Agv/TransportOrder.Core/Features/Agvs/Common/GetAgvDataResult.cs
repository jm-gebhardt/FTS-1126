using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for the GetAgvData request
/// </summary>
public record GetAgvDataResult(AgvDataDto AgvData);