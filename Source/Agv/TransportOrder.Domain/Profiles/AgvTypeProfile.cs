using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Mapping for AGV types.
/// </summary>
[UsedImplicitly]
public class AgvTypeProfile : Profile
{
    /// <inheritdoc/>
    public AgvTypeProfile()
    {
        CreateMap<AgvTypeInfo, AgvType>()
            .ForMember(a => a.Id, opt => opt.Ignore());
    }
}