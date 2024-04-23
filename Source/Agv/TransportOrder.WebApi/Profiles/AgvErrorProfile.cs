using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

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