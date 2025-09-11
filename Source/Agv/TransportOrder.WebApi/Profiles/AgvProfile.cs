using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for AgvDto
/// </summary>
public class AgvProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvProfile()
    {
        CreateMap<Messages.MessageContent.Agv, AgvDto>().ReverseMap();
        CreateMap<AgvData, AgvDataDto>().ReverseMap();
        CreateMap<Messages.MessageContent.Agv, AgvStateDto>();
        CreateMap<Messages.MessageContent.Maintenance, MaintenanceDto>();
        CreateMap<MaintenanceLog, AgvMaintenanceLogDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentRouteDto>();
        CreateMap<CurrentAgvRouteReadModel, CurrentAgvRouteDto>()
            .ForMember(c => c.CurrentRoute, c => c.MapFrom(o => o));
        CreateMap<AgvState, AgvStateType>();

        CreateMap<AgvBatteryStatus, AgvBatteryStatusDto>(MemberList.Source);
        CreateMap<Messages.MessageContent.AgvSettings, AgvBatteryStatusDto>(MemberList.Source)
            .ForSourceMember(d => d.AgvOfflineTimeoutMsWhileIdle, o => o.DoNotValidate())
            .ForSourceMember(d => d.AgvOfflineTimeoutMsWhileBusy, o => o.DoNotValidate());
        CreateMap<(AgvBatteryStatus, Messages.MessageContent.AgvSettings), AgvBatteryStatusDto>()
            .IncludeMembers(s => s.Item1, s => s.Item2);
        CreateMap<AgvSettingsReadModel, AgvBatteryStatusDto>(MemberList.Source)
            .ForSourceMember(d => d.SectionName, o => o.DoNotValidate())
            .ForSourceMember(d => d.AgvOfflineTimeoutMsWhileIdle, o => o.DoNotValidate())
            .ForSourceMember(d => d.AgvOfflineTimeoutMsWhileBusy, o => o.DoNotValidate());
        CreateMap<(AgvBatteryStatus, AgvSettingsReadModel), AgvBatteryStatusDto>()
            .IncludeMembers(s => s.Item1, s => s.Item2);
        CreateMap<AgvMode, AgvModeDto>().ReverseMap();
        CreateMap<AgvOperatingDataStatus, AgvOperatingDataStatusDto>();
    }
}