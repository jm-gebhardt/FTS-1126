using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for GetAgvErrors request
/// </summary>
/// <param name="AgvErrorsDto"></param>
public record GetActiveAgvErrorsResult(List<AgvErrorDto> AgvErrorsDto);