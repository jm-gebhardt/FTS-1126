using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;

/// <summary>
/// Base Implementation of IChargingService
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class BaseAgvChargingService : IChargingService
{
    private readonly ILogger<BaseAgvChargingService> _logger;
    private readonly IAgvRegistry _agvRegistry;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;

    /// <summary>
    /// Create a new instance of <see cref="BaseAgvChargingService"/>
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="agvRegistry"></param>
    /// <param name="settingsProvider"></param>
    public BaseAgvChargingService(
        ILogger<BaseAgvChargingService> logger,
        IAgvRegistry agvRegistry,
        ISettingsProvider<AgvSettings> settingsProvider)
    {
        _logger = logger;
        _agvRegistry = agvRegistry;
        _settingsProvider = settingsProvider;
    }

    /// <inheritdoc/>
    public bool AgvNeedsCharging(IAgv agv)
    {
        if (agv.AgvStatus == null)
        {
            _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", agv.CorrelationId);
            return false;
        }

        var agvSettings = _settingsProvider.GetSettings();

        return agv.AgvStatus.BatteryCharge <= agvSettings.BatteryLevelCritical;
    }

    /// <inheritdoc/>
    public bool AgvIsEligibleForCharging(IAgv agv)
    {
        if (agv.AgvStatus == null)
        {
            _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", agv.CorrelationId);
            return false;
        }

        var agvSettings = _settingsProvider.GetSettings();

        return agv.AgvStatus.BatteryCharge <= agvSettings.BatteryLevelLow;
    }

    /// <inheritdoc/>
    public List<IAgv> GetChargingAgvsWithGoodBatteryLevel()
    {
        List<IAgv> agvs = _agvRegistry.GetAllAgvs().ToList();
        List<IAgv> agvWithGoodLevel = agvs.Where(agv => 
                agv.AgvStatus is {Charging: true} && 
                agv.AgvStatus.BatteryCharge > _settingsProvider.GetSettings().BatteryLevelLow
            )
            .ToList();
            
        return agvWithGoodLevel;
    }
}