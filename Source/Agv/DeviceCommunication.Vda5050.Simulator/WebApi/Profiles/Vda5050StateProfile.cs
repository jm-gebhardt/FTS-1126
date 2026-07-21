using AutoMapper;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Profiles;

/// <summary>
/// Provides a profile mapping the VDA5050 state to its dto.
/// </summary>
[UsedImplicitly]
public class Vda5050StateProfile : Profile
{
    /// <inheritdoc/>
    public Vda5050StateProfile()
    {
        CreateMap<MqttState, Vda5050StateDto>()
            .ForMember(dest => dest.CorrelationId, opt => opt.Ignore())
            .ForMember(dest => dest.MessageCreatedDate, opt => opt.MapFrom(src => src.Timestamp.UtcDateTime));
        CreateMap<MqttMap, MapDto>();
        CreateMap<MqttNodeState, NodeStateDto>();
        CreateMap<MqttNodePosition, NodePositionDto>();
        CreateMap<MqttEdgeState, EdgeStateDto>();
        CreateMap<MqttTrajectory, TrajectoryDto>();
        CreateMap<MqttControlPoint, ControlPointDto>();
        CreateMap<MqttLoad, LoadDto>();
        CreateMap<MqttBoundingBoxReference, BoundingBoxReferenceDto>();
        CreateMap<MqttLoadDimensions, LoadDimensionsDto>();
        CreateMap<MqttActionState, ActionStateDto>();
        CreateMap<MqttBatteryState, BatteryStateDto>();
        CreateMap<MqttError, ErrorDto>();
        CreateMap<MqttErrorReference, ErrorReferenceDto>();
        CreateMap<MqttInformation, InformationDto>();
        CreateMap<MqttInfoReference, InfoReferenceDto>();
        CreateMap<MqttSafetyState, SafetyStateDto>();
        CreateMap<MqttAgvPosition, Vda5050AgvPositionDto>();
        CreateMap<MqttVelocity, VelocityDto>();
    }
}