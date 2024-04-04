using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles.Resolver;

/// <summary>
/// Provides a converter type converter for converting <see cref="AgvSettingsDto"/> to <see cref="AgvSettingsReadModel"/>.
/// </summary>
[UsedImplicitly]
public class AgvSettingsDtoToReadModelConverter : ITypeConverter<AgvSettingsDto, AgvSettingsReadModel>
{
    /// <inheritdoc />
    public AgvSettingsReadModel Convert(AgvSettingsDto source, AgvSettingsReadModel destination, ResolutionContext context)
    {
        return new AgvSettingsReadModel
        {
            BatteryLevelCritical = source.BatteryLevelCritical ?? -1,
            BatteryLevelLow = source.BatteryLevelLow ?? -1,
            MinChargeUp = source.MinChargeUp ?? -1,
            MaxChargeUp = source.MaxChargeUp ?? -1
        };
    }
}


