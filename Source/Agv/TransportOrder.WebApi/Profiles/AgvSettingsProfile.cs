using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for agv settings.
/// </summary>
public class AgvSettingsProfile : WebApiProfile
{
    /// <inheritdoc />
    public AgvSettingsProfile()
    {
        CreateMap<AgvSettings, AgvSettingsResponseDto>();
        CreateMap<AgvSettingsReadModel, AgvSettingsResponseDto>()
            .ForMember(d => d.CorrelationId, opt => opt.Ignore());

        CreateMap<AgvSettingsDto, AgvSettingsReadModel>().ConvertUsing<AgvSettingsDtoToReadModelConverter>();
    }
}