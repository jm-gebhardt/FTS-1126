using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using TransportOrder.Domain.ReadModels;

namespace TransportOrder.Messages.Profiles;


/// <summary>
/// Profile for Agv message content
/// </summary>
public class AgvProfile : Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<AgvStatus, MessageContent.Agv>(MemberList.None)
            .ForMember(nameof(MessageContent.Agv.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(MessageContent.Agv.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(MessageContent.Agv.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(MessageContent.Agv.BadBatteryLevel), opt => opt.MapFrom(src => AgvStatus.BadBatteryLevel))
            .ForMember(nameof(MessageContent.Agv.GoodBatteryLevel), opt => opt.MapFrom(src => AgvStatus.GoodBatteryLevel));

        CreateMap<IAgv, MessageContent.Agv>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus);

        CreateMap<ActionGroupReadModel, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.CurrentActionGroup, opt => opt.MapFrom(s => s))
            // ignore CorrelationId, as it's set from the other mapping already, otherwise this would overwrite the CorrelationId of the AgvDto (Agv) with the CorrelationId of the ActionGroup
            .ForMember(x => x.CorrelationId, opt => opt.Ignore());

        CreateMap<IList<AgvError>, MessageContent.Agv>(MemberList.None)
             .ForMember(nameof(MessageContent.Agv.Errors), opt => opt.MapFrom(s => s));

        CreateMap<AgvData, Requests.Agv.AgvData>();
    }
}