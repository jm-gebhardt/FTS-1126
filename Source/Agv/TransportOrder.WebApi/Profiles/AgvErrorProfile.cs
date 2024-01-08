using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="AgvError" />
/// </summary>
public class AgvErrorProfile : WebApiProfile
{
    /// <inheritdoc cref="AgvErrorProfile" />
    public AgvErrorProfile()
    {
        CreateMap<AgvError, AgvErrorDto>();
        CreateMap<Messages.Requests.Agv.Queries.AgvError, AgvErrorDto>();
        CreateMap<List<AgvError>, AgvErrorStatusDto>()
            .ForMember(c => c.Errors, c => c.MapFrom(s => s))
            .ForMember(c => c.CorrelationId, c => c.Ignore());
        CreateMap<Guid, AgvErrorStatusDto>(MemberList.None)
            .ForMember(c => c.CorrelationId, c => c.MapFrom(s => s));
    }
}