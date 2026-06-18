using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;

/// <summary>
/// Provides mapping for <see cref="MqttOrder"/>
/// </summary>
[UsedImplicitly]
public class OrderProfile : Profile
{
    /// <inheritdoc cref="OrderProfile" />
    public OrderProfile()
    {
        CreateMap<OrderActionParameter, MqttActionParameter>();
        CreateMap<OrderAction, MqttOrderAction>();
        CreateMap<OrderActionState, MqttActionState>().ReverseMap();
        CreateMap<VehicleActionStatus, MqttActionStatus>().ReverseMap();
        CreateMap<Order, MqttOrder>()
            .ForMember(m => m.Manufacturer, opt => opt.MapFrom(o => o.AssignedAgv!.Manufacturer))
            .ForMember(m => m.SerialNumber, opt => opt.MapFrom(o => o.AssignedAgv!.SerialNumber))
            .ForMember(m => m.ZoneSetId, opt => opt.Ignore())
            .ForMember(m => m.Version, opt => opt.MapFrom(o => Vda5050Specifications.Version))
            .ForMember(m => m.Nodes, opt => opt.MapFrom(o => o.Route.Nodes))
            .ForMember(m => m.Edges, opt => opt.MapFrom(o => o.Route.Edges))
            .ForMember(m => m.HeaderId, opt => opt.Ignore())
            .ForMember(m => m.Timestamp, opt => opt.Ignore())
            .ForSourceMember(o => o.PlannedRoute, opt => opt.DoNotValidate())
            .ForSourceMember(o => o.Route, opt => opt.DoNotValidate())
            .ForSourceMember(o => o.AssignedAgv, opt => opt.DoNotValidate());

        CreateMap<OrderNode, MqttOrderNode>();
        CreateMap<OrderNodePosition, MqttOrderNodePosition>();
        CreateMap<OrderEdge, MqttOrderEdge>();
        CreateMap<OrientationType, MqttOrientationType>();
        CreateMap<Trajectory, MqttTrajectory>();
        CreateMap<ControlPoint, MqttControlPoint>();
        CreateMap<Corridor, MqttCorridor>();
        CreateMap<CorridorRefPoint, MqttCorridorRefPoint>();
    }
}