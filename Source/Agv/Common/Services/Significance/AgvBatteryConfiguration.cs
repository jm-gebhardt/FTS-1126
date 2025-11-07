using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>Thresholds for AgvBatteryStatus comparison.</summary>
/// <param name="BatteryChargeThreshold">Used to compare BatteryCharge [%].</param>
/// <param name="BatteryVoltageThreshold">Used to compare BatteryVoltage [V].</param>
/// <param name="BatteryCurrentThreshold">Used to compare BatteryCurrent [A].</param>
/// <param name="BatteryTemperatureThreshold">Used to compare BatteryTemperature [°C].</param>
public record AgvBatteryConfiguration(double BatteryChargeThreshold, double BatteryVoltageThreshold, double BatteryCurrentThreshold, double BatteryTemperatureThreshold) : FilterConfiguration;