using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for GetAgvErrorHistory request
/// </summary>
/// <param name="AgvErrorsDto"></param>
public record GetAgvErrorsResult(List<AgvErrorDto> AgvErrorsDto);