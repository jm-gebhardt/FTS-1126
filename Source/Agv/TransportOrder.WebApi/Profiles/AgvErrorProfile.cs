using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
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
        CreateMap<AgvError, AgvErrorDto>()
            .ForSourceMember(s => s.ErrorFlag, opt => opt.DoNotValidate());
        CreateMap<Messages.Requests.Agv.Queries.AgvError, AgvErrorDto>();
        CreateMap<List<AgvError>, AgvErrorStatusDto>()
            .ForMember(c => c.Errors, c => c.MapFrom(s => s))
            .ForMember(c => c.CorrelationId, c => c.Ignore());
        CreateMap<Guid, AgvErrorStatusDto>(MemberList.None)
            .ForMember(c => c.CorrelationId, c => c.MapFrom(s => s));
        CreateMap<AgvErrors, AgvErrorsDto>()
            .ForMember(c => c.AgvErrors, c => c.MapFrom(s => s.AgvErrorList));
    }
}