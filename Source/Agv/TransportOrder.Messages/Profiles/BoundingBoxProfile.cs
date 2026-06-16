using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// AutoMapper profile for bounding box message content.
/// </summary>
[UsedImplicitly]
public class BoundingBoxProfile : Profile
{
    /// <inheritdoc cref="BoundingBoxProfile"/>
    public BoundingBoxProfile()
    {
        CreateMap<ResourceBoundingBox, ResourceBoundingBoxContent>(MemberList.Destination)
            .ForMember(dest => dest.Polygon, opt => opt.MapFrom(src => src.Polygon.Points));
    }
}