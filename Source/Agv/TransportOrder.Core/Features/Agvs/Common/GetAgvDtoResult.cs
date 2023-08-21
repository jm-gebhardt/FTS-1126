using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Defines a result for the <see cref="GetAgvDtoRequest"/>
/// </summary>
/// <param name="Agv">The AgvDto</param>
public record GetAgvDtoResult(AgvDto Agv);