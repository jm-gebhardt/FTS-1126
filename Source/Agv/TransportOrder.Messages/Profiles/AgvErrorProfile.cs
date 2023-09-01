using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for <see cref="AgvError" />
/// </summary>
public class AgvErrorProfile : Profile
{
    /// <inheritdoc cref="AgvErrorProfile" />
    public AgvErrorProfile()
    {
        CreateMap<AgvError, Requests.Agv.AgvError>();

        CreateMap<Guid, AgvError>(MemberList.None)
            .ForMember(nameof(AgvError.AgvCorrelationId), opt => opt.MapFrom(src => src));
    }
}