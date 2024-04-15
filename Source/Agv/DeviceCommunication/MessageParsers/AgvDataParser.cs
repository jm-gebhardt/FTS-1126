using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;

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
    [PublicAPI]
    public bool SerialNumberValid => !string.IsNullOrEmpty(_data.SerialNumber);

    /// <summary>
    /// Returns the serial number of the AGV.
    /// </summary>
    /// <exception cref="InvalidOperationException">Serial number is missing.</exception>
    [PublicAPI]
    public string SerialNumber
    {
        get
        {
            if (!SerialNumberValid)
            {
                throw new SerialNumberNotSetException(nameof(MqttGraph));
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
            _data.Model ?? throw new ArgumentNullException(nameof(MqttAgvData.Model)),
            _data.Type ?? throw new ArgumentNullException(nameof(MqttAgvData.Type)),
            _data.IpV4Address ?? throw new ArgumentNullException(nameof(MqttAgvData.IpV4Address)),
            _data.SoftwareVersion ?? "v0",
            _data.BatteryCapacity,
            _data.Acceleration ?? throw new ArgumentNullException(nameof(MqttAgvData.Acceleration)),
            _data.MaxVelocityStraight,
            _data.MaxVelocityTurn ?? throw new ArgumentNullException(nameof(MqttAgvData.MaxVelocityTurn)),
            _data.MaxVelocityTurnOnSpot ?? throw new ArgumentNullException(nameof(MqttAgvData.MaxVelocityTurnOnSpot)),
            _data.TurnOnSpotThreshold ?? throw new ArgumentNullException(nameof(MqttAgvData.TurnOnSpotThreshold)),
            _data.Width ?? throw new ArgumentNullException(nameof(MqttAgvData.Width)),
            _data.Length ?? throw new ArgumentNullException(nameof(MqttAgvData.Length)),
            _data.Payload ?? throw new ArgumentNullException(nameof(MqttAgvData.Payload)),
            Sensors.ToArray()
        );
}