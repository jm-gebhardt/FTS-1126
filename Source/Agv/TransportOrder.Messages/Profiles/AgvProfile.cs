using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using MassTransit.Serialization;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Profile for Agv message content
/// </summary>
public class AgvProfile : Profile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<AgvStatus, Agv>(MemberList.None)
            .ForMember(nameof(Agv.IsDisturbed), opt => opt.MapFrom(src => src.Disturbed))
            .ForMember(nameof(Agv.IsAutomatic), opt => opt.MapFrom(src => src.Automatic))
            .ForMember(nameof(Agv.IsCharging), opt => opt.MapFrom(src => src.Charging))
            .ForMember(nameof(Agv.Zones), opt => opt.MapFrom(src => src.ZoneIds));

        CreateMap<AgvSettingsReadModel, AgvSettings>()
            .ForMember(d => d.CorrelationId, opt => opt.Ignore());

        CreateMap<Guid, AgvSettings>(MemberList.None)
            .ForMember(d => d.CorrelationId, opt => opt.MapFrom(s => s));

        CreateMap<Domain.Models.Agv.Agv, Agv>(MemberList.None)
            .ConstructUsing((src, ctx) =>
            {
                ctx.TryGetItems(out Dictionary<string, object>? items);
                items.TryGetValue(nameof(Agv.CurrentActionGroup), out ActionGroupReadModel? currentActionGroup);
                items.TryGetValue(nameof(Agv.Errors), out List<AgvError> agvErrors);
                agvErrors ??= [];
                items.TryGetValue(nameof(Agv.LastNode), out GraphNode? lastNode);
                items.TryGetValue(nameof(Agv.LastMaintenance), out MaintenanceLog? lastMaintenance);

                var agv = new Agv(
                    src.CorrelationId,
                    src.SerialNumber,
                    src.Name,
                    src.CurrentAgvState,
                    src.TimeOfStateTransition,
                    src.TimeOfLockStateChanged,
                    src.IsOnline,
                    src.IsLocked,
                    (src.AgvStatus?.Disturbed).GetValueOrDefault(false),
                    (src.AgvStatus?.Automatic).GetValueOrDefault(false),
                    (src.AgvStatus?.BatteryCharge).GetValueOrDefault(0),
                    (src.AgvStatus?.Charging).GetValueOrDefault(false),
                    currentActionGroup,
                    agvErrors,
                    ctx.Mapper.Map<MessageContent.GraphNode>(lastNode),
                    src.Mode,
                    src.AgvStatus?.ZoneIds?.ToList() ?? [],
                    src.AgvStatus?.StationIds?.ToList() ?? [],
                    src.AgvStatus?.TotalDistance ?? 0,
                    src.AgvStatus?.TotalLifetime ?? 0,
                    ctx.Mapper.Map<Maintenance>(lastMaintenance)
                );
                return agv;
            });

        CreateMap<MaintenanceLog, Maintenance>()
            .ForSourceMember(s => s.User, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.AgvCorrelationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.CreationDate, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Comment, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());

        CreateMap<AgvData, Requests.Agv.Queries.AgvData>();
    }
}