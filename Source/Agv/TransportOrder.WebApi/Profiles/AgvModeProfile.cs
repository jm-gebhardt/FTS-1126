using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvModeProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvModeProfile()
    {
        CreateMap<AgvMode, AgvModeDto>().ReverseMap();
    }
}