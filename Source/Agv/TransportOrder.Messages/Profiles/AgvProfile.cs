using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvSettings;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Models.Graphs.GraphNode;

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
            .ForMember(nameof(MessageContent.Agv.LastNodeId), opt => opt.MapFrom(src => src.LastNodeId));

        CreateMap<Domain.Models.Agv.AgvStatus, MessageContent.Agv>(MemberList.None)
            .ForMember(nameof(MessageContent.Agv.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(MessageContent.Agv.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(MessageContent.Agv.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(MessageContent.Agv.LastNodeId), opt => opt.MapFrom(src => src.LastNodeId));

        CreateMap<Guid, AgvSettings>(MemberList.None)
            .ForMember(d => d.CorrelationId, opt => opt.MapFrom(s => s));
        CreateMap<Domain.Models.Settings.AgvSettingsReadModel, AgvSettings>()
            .ForMember(d => d.CorrelationId, opt => opt.Ignore());

        CreateMap<Domain.Models.Agv.Agv, MessageContent.Agv>(MemberList.None)
            .IncludeMembers(s => s.AgvStatus)
            .ForMember(nameof(MessageContent.Agv.CurrentState), opt => opt.MapFrom(src => src.CurrentAgvState));

        CreateMap<ActionGroupReadModel, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.CurrentActionGroup, opt => opt.MapFrom(s => s))
            // ignore CorrelationId, as it's set from the other mapping already, otherwise this would overwrite the CorrelationId of the AgvDto (Agv) with the CorrelationId of the ActionGroup
            .ForMember(x => x.CorrelationId, opt => opt.Ignore());

        CreateMap<GraphNode, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.LastNode, opt => opt.MapFrom(s => s));

        CreateMap<IList<AgvError>, MessageContent.Agv>(MemberList.None)
            .ForMember(d => d.Errors, opt => opt.MapFrom(s => s));

        CreateMap<AgvData, Requests.Agv.Queries.AgvData>();
    }
}