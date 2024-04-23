using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for <see cref="AgvError" />
/// </summary>
public class AgvErrorProfile : Profile
{
    /// <inheritdoc cref="AgvErrorProfile" />
    public AgvErrorProfile()
    {
        CreateMap<AgvError, Requests.Agv.Queries.AgvError>();

        CreateMap<Guid, AgvError>(MemberList.None)
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.MapFrom(src => src));
    }
}