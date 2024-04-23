using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles.Resolver;
using AgvSettings = Gebhardt.Acc.TransportOrder.Messages.MessageContent.AgvSettings;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

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