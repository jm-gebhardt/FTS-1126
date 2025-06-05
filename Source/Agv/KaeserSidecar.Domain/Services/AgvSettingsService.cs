using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <inheritdoc cref="IAgvSettingsService"/>>
public class AgvSettingsService(ITransportOrderAgvSettingsProvider agvSettingsProvider, ITransportOrderAgvRepository agvRepository) : IAgvSettingsService
{
    /// <inheritdoc cref="IAgvSettingsService"/>>
    public async Task<bool> CheckAgvHasCriticalBatteryLevel(Guid agvCorrelationId)
    {
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);
        await agvSettingsProvider.InitializeSettingsGroups();
        var settings = agvSettingsProvider.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        return agv.AgvStatus!.BatteryCharge <= settings.BatteryLevelCritical;
    }
}