using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;

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

        CreateMap<Domain.Models.Agv.AgvStatus, MessageContent.Agv>(MemberList.None)
            .ForMember(nameof(MessageContent.Agv.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(MessageContent.Agv.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(MessageContent.Agv.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(MessageContent.Agv.BadBatteryLevel), opt => opt.MapFrom(src => Domain.Models.Agv.AgvStatus.BadBatteryLevel))
            .ForMember(nameof(MessageContent.Agv.GoodBatteryLevel), opt => opt.MapFrom(src => Domain.Models.Agv.AgvStatus.GoodBatteryLevel));

        CreateMap<Domain.Models.Agv.Agv, MessageContent.Agv>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus)
            .ForMember(nameof(MessageContent.Agv.CurrentState), opt => opt.MapFrom(src => src.CurrentAgvState));

        CreateMap<ActionGroupReadModel, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.CurrentActionGroup, opt => opt.MapFrom(s => s))
            // ignore CorrelationId, as it's set from the other mapping already, otherwise this would overwrite the CorrelationId of the AgvDto (Agv) with the CorrelationId of the ActionGroup
            .ForMember(x => x.CorrelationId, opt => opt.Ignore());

        CreateMap<IList<AgvError>, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.Errors, opt => opt.MapFrom(s => s));

        CreateMap<AgvData, Requests.Agv.Queries.AgvData>();
    }
}