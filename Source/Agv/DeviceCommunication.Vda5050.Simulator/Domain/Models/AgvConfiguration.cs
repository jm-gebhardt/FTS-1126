using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// The configuration for a simulated agv.
/// </summary>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="MapId">The map the agv is on.</param>
/// <param name="Manufacturer">The agv manufacturer</param>
/// <param name="Speed">The maximum speed of the agv on straight edges.</param>
public record AgvConfiguration(string SerialNumber, string MapId, string Manufacturer, float Speed = 1)
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId = SerialNumber.ToGuid();

    /// <summary>
    /// The id of the agv type the agv belongs to.
    /// </summary>
    public Guid AgvTypeId { get; init; }

    /// <summary>
    /// The interval in milliseconds between two state publications.
    /// </summary>
    public int StateUpdateIntervalMs { get; init; } = SimulatedAgvTypeConfiguration.DefaultStateUpdateIntervalMs;

    /// <summary>
    /// The interval in milliseconds between two visualization publications.
    /// </summary>
    public int VisualizationUpdateIntervalMs { get; init; } = SimulatedAgvTypeConfiguration.DefaultVisualizationUpdateIntervalMs;

    /// <summary>
    /// The battery discharge rate in percent per second.
    /// </summary>
    public double BatteryDischargeRate { get; init; } = SimulatedAgvTypeConfiguration.DefaultBatteryDischargeRate;

    /// <summary>
    /// The battery charge rate in percent per second.
    /// </summary>
    public double BatteryChargeRate { get; init; } = SimulatedAgvTypeConfiguration.DefaultBatteryChargeRate;

    /// <summary>
    /// The battery charge in percent the agv starts with. Applied when the agv is spawned.
    /// </summary>
    public int InitialBatteryCharge { get; init; } = SimulatedAgvTypeConfiguration.DefaultInitialBatteryCharge;
}

/// <summary>
/// Provides extension methods for <see cref="AgvConfiguration"/>
/// </summary>
public static class AgvConfigurationExtension
{
    extension(AgvConfiguration configuration)
    {
        /// <summary>
        /// Get the routing key for this agv.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>Routing key:  interfaceName.version.manufacturer.serialNumber.topic</returns>
        public string GetRoutingKey(string topic)
        {
            return $"uagv.v2.{configuration.Manufacturer}.{configuration.SerialNumber}.{topic}";
        }
    }
}