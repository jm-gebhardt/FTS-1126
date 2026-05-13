using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// The configuration for a simulated agv.
/// </summary>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="Manufacturer">The agv manufacturer</param>
/// <param name="MapId">The map the agv is on.</param>
/// <param name="Speed">The maximum speed of the agv on straight edges.</param>
public record AgvConfiguration(string SerialNumber, string MapId="map", string Manufacturer = "gebhardt", float Speed=1)
{
    /// <summary>
    /// Correlation id of the agv.
    /// </summary>
    public Guid CorrelationId = SerialNumber.ToGuid();
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