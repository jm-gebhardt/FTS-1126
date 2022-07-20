using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers
{
    /// <summary>
    /// Parse <see cref="MqttAgvData"/> message.
    /// </summary>
    public class AgvDataParser
    {
        private readonly MqttAgvData _data;
        private List<AgvData.Sensor>? _sensors;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data"><see cref="MqttAgvData"/> message.</param>
        public AgvDataParser(MqttAgvData data)
        {
            _data = data;
        }

        /// <summary>
        /// Returns true when the serial number is present otherwise false.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool SerialNumberValid => !string.IsNullOrEmpty(_data.SerialNumber);

        /// <summary>
        /// Returns the serial number of the AGV.
        /// </summary>
        /// <exception cref="InvalidOperationException">Serial number is missing.</exception>
        [JetBrains.Annotations.PublicAPI]
        public string SerialNumber
        {
            get
            {
                if (!SerialNumberValid)
                {
                    throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, serialNumber must not be null or empty.");
                }
                return _data.SerialNumber;
            }
        }

        /// <summary>
        /// Parse sensors
        /// </summary>
        private void ParseSensors()
        {
            if (_sensors == null)
            {
                _sensors = new List<AgvData.Sensor>();
                foreach (var sensor in _data.Sensors)
                {
                    _sensors.Add(new AgvData.Sensor
                    {
                        SensorIdentifier = sensor.SensorIdentifier
                    });
                }
            }
        }

        /// <summary>
        /// Returns the list of sensors
        /// </summary>
        private List<AgvData.Sensor> Sensors
        {
            get
            {
                ParseSensors();
                return _sensors!;
            }
        }

        /// <summary>
        /// Create AgvGraph
        /// </summary>
        public AgvData AgvData =>
            new(
                SerialNumber.ToGuid(),
                _data.Model,
                _data.Type,
                _data.IpV4Address,
                _data.MaxVelocityStraight,
                _data.BatteryCapacity,
                _data.Acceleration,
                _data.Length,
                _data.Width,
                _data.MaxVelocityTurn,
                _data.MaxVelocityTurnOnSpot,
                _data.Payload,
                Sensors.ToArray()
            );
    }
}
