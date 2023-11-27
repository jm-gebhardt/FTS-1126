using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="AgvError" />
/// </summary>
public class AgvErrorProfile : Profile
{
    /// <inheritdoc cref="AgvErrorProfile" />
    public AgvErrorProfile()
    {
        AddGlobalIgnore(nameof(DatedMessage.MessageCreatedDate));
        CreateMap<AgvError, AgvErrorDto>();
        CreateMap<Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries.AgvError, AgvErrorDto>();
        CreateMap<List<AgvError>, AgvErrorStatusDto>()
            .ForMember(c => c.Errors, c => c.MapFrom(s => s))
            .ForMember(c => c.CorrelationId, c => c.Ignore());
        CreateMap<Guid, AgvErrorStatusDto>(MemberList.None)
            .ForMember(c => c.CorrelationId, c => c.MapFrom(s => s));
    }
}