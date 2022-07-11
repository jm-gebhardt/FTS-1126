using Gebhardt.StoreWare.Agv.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// The basic AGV data from the AGV
    /// </summary>
    [PublicAPI]
    [EntityName("amq.topic")]
    [RoutingKey("agv.agvData")]
    public interface IAgvData
    {
        /// <summary>
        /// The maximum velocity of the AGV while turning on spot [m/s^2]
        /// </summary>
        public double? Acceleration { get; set; }

        /// <summary>
        /// The capacity of the AGVs battery in [Ah]
        /// </summary>
        public double BatteryCapacity { get; set; }

        /// <summary>
        /// The IP Address of the AGV
        /// </summary>
        public string IpV4Address { get; set; }

        /// <summary>
        /// The length of the AGV in [m]
        /// </summary>
        public double? Length { get; set; }

        /// <summary>
        /// The maximum velocity of the AGV in straight direction [m/s]
        /// </summary>
        public double MaxVelocityStraight { get; set; }

        /// <summary>
        /// The maximum velocity of the AGV in while turning [m/s]
        /// </summary>
        public double? MaxVelocityTurn { get; set; }

        /// <summary>
        /// The maximum velocity of the AGV while turning on spot [rad/s]
        /// </summary>
        public double? MaxVelocityTurnOnSpot { get; set; }

        /// <summary>
        /// The model name of the AGV
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// The max. payload of the AGV in [kg]
        /// </summary>
        public long? Payload { get; set; }

        /// <summary>
        /// The list of sensors in the agv
        /// </summary>
        public Sensor[] Sensors { get; set; }

        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// The type of the AGV
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The width of the AGV in [m]
        /// </summary>
        public double? Width { get; set; }
    }

    /// <summary>
    /// A sensor object skeleton
    /// </summary>
	[PublicAPI]
    public class Sensor
    {
        /// <summary>
        /// The identifier of the sensor
        /// </summary>
        public string? SensorIdentifier { get; set; }
    }
}

