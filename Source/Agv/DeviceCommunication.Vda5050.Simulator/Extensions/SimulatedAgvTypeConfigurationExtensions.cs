using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Extensions;

/// <summary>
/// Provides extension methods for <see cref="SimulatedAgvTypeConfiguration"/>
/// </summary>
public static class SimulatedAgvTypeConfigurationExtensions
{
    private static int AgvIndex(SimulatedAgvTypeConfiguration configuration, string serialNumber)
    {
        string suffix = serialNumber.Length > configuration.SeriesName.Length ? serialNumber[configuration.SeriesName.Length..] : string.Empty;
        return int.TryParse(suffix, out int index) ? index : 0;
    }

    private static int SpreadInitialBatteryCharge(SimulatedAgvTypeConfiguration configuration, string serialNumber)
    {
        int span = 2 * configuration.InitialBatteryChargeDeviation + 1;
        int step = AgvIndex(configuration, serialNumber) % span;
        int offset = step % 2 == 0 ? step / 2 : -((step + 1) / 2);
        return Math.Clamp(configuration.InitialBatteryCharge + offset,
            SimulationLimits.MinBatteryChargePercent,
            SimulationLimits.MaxBatteryChargePercent);
    }

    /// <param name="configuration">The simulated AGV type configuration.</param>
    extension(SimulatedAgvTypeConfiguration configuration)
    {
        /// <summary>
        /// Converts the simulated AGV type configuration to an AGV configuration.
        /// </summary>
        /// <param name="serialNumber">The serial number of the AGV.</param>
        /// <param name="mapId">The ID of the map.</param>
        /// <returns>The AGV configuration.</returns>
        public AgvConfiguration ToAgvConfiguration(string serialNumber, string mapId)
        {
            return new AgvConfiguration(serialNumber, mapId, configuration.Manufacturer)
            {
                AgvTypeId = configuration.Id,
                AgvTypeIdentifier = configuration.AgvTypeIdentifier,
                StateUpdateIntervalMs = configuration.StateUpdateIntervalMs,
                VisualizationUpdateIntervalMs = configuration.VisualizationUpdateIntervalMs,
                BatteryDischargeRate = configuration.BatteryDischargeRate,
                BatteryChargeRate = configuration.BatteryChargeRate,
                InitialBatteryCharge = SpreadInitialBatteryCharge(configuration, serialNumber)
            };
        }

        /// <summary>
        /// Creates a copy of the simulated AGV type configuration with the given settings applied.
        /// </summary>
        /// <param name="settings">The settings to apply. A setting that is null keeps the current value.</param>
        /// <returns>The simulated AGV type configuration with the settings applied.</returns>
        public SimulatedAgvTypeConfiguration ApplySettings(AgvTypeSimulationSettings settings)
        {
            return configuration with
            {
                NoOfAgvs = settings.NoOfAgvs ?? configuration.NoOfAgvs,
                StateUpdateIntervalMs = settings.StateUpdateIntervalMs ?? configuration.StateUpdateIntervalMs,
                VisualizationUpdateIntervalMs = settings.VisualizationUpdateIntervalMs ?? configuration.VisualizationUpdateIntervalMs,
                BatteryDischargeRate = settings.BatteryDischargeRate ?? configuration.BatteryDischargeRate,
                BatteryChargeRate = settings.BatteryChargeRate ?? configuration.BatteryChargeRate,
                InitialBatteryCharge = settings.InitialBatteryCharge ?? configuration.InitialBatteryCharge,
                InitialBatteryChargeDeviation = settings.InitialBatteryChargeDeviation ?? configuration.InitialBatteryChargeDeviation
            };
        }
    }
}