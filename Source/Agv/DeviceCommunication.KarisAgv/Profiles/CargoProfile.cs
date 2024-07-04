using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;

/// <summary>
/// Profile for MqttLoad
/// </summary>
[UsedImplicitly]
public class CargoProfile : Profile
{
    /// <inheritdoc cref="CargoProfile" />
    public CargoProfile()
    {
        CreateMap<MqttLoad, Cargo>()
            .ForMember(c => c.CorrelationId, c => c.MapFrom(l => l.LoadId!.ToGuid()))
            // AgvCorrelationId is mapped in the next mapping (below)
            .ForMember(c => c.AgvCorrelationId, c => c.Ignore())
            .ForMember(c => c.Dimension, c => c.MapFrom(l => l.LoadDimensions))
            .ForMember(c => c.CenterPointPosition, c => c.MapFrom(l => l.BoundingBoxReference));
        // for AgvCorrelationId
        CreateMap<Guid, Cargo>(MemberList.None)
            .ForMember(c => c.AgvCorrelationId, opt => opt.MapFrom(src => src));

        CreateMap<MqttLoadDimensions, Cuboid>();
        CreateMap<MqttBoundingBoxReference, Point3D>();
    }
}