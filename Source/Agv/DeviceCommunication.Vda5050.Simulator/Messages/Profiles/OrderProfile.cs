using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Profiles;

/// <summary>
/// Provides mapping for <see cref="MqttOrder"/>
/// </summary>
[PublicAPI]
public class OrderProfile : Profile
{
    /// <inheritdoc cref="OrderProfile" />
    public OrderProfile()
    {
        CreateMap<MqttActionParameter,OrderActionParameter>();
        CreateMap<MqttOrderAction, OrderAction>();
        CreateMap<OrderAction, MqttOrderAction>();
        CreateMap<OrderActionState, MqttActionState>().ReverseMap();
        CreateMap<VehicleActionStatus, MqttActionStatus>().ReverseMap();
        CreateMap<MqttOrder, Order>()
            .ForMember(o => o.FullRoute, opt => opt.MapFrom((src, _, _, ctx) =>
                new Route(
                    ctx.Mapper.Map<ICollection<OrderNode>>(src.Nodes),
                    ctx.Mapper.Map<ICollection<OrderEdge>>(src.Edges))))
            .ForSourceMember(m => m.HeaderId, opt => opt.DoNotValidate())
            .ForSourceMember(m => m.Timestamp, opt => opt.DoNotValidate())
            .ForMember(o => o.State, opt => opt.Ignore())
            .ForMember(o => o.Base, opt => opt.Ignore())
            .ForMember(o => o.Duration, opt => opt.Ignore());

        CreateMap<MqttOrderNode, OrderNode>();
        CreateMap<MqttOrderNodePosition, OrderNodePosition>();
        CreateMap<MqttOrderEdge, OrderEdge>()
            .ForMember(e => e.EstimatedTravelTime, opt => opt.Ignore())
            .ForMember(e => e.GraphEdge, opt => opt.Ignore());
        CreateMap<MqttOrientationType, OrientationType>();
        CreateMap<MqttTrajectory, Trajectory>();
        CreateMap<MqttControlPoint, ControlPoint>();
        CreateMap<MqttCorridor, Corridor>();
        CreateMap<MqttCorridorRefPoint, CorridorRefPoint>();
    }
}