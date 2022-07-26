using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// A list of all stations.
    /// <para>See "station_list.schema".</para>
    /// </summary>
	[JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]       // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.stationList")] // Specifies the topic where subscribers can receive the message
    public record MqttStationList
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// The modification date of the station list. Timestamp in ISO8601 format.
        /// </summary>
        public DateTimeOffset ModificationDate { get; }

        /// <summary>
        /// The unique ID replied from the request.
        /// </summary>
        public Guid? RequestId { get; set; }

        /// <summary>
        /// Station list
        /// </summary>
        public Station[] Stations { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqttStationList"/>
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="modificationDate"></param>
        /// <param name="stations"></param>
        public MqttStationList(string serialNumber, DateTimeOffset modificationDate, Station[] stations)
        {
            SerialNumber = serialNumber;
            ModificationDate = modificationDate;
            Stations = stations;
        }
    }

    /// <summary>
    /// AGV station
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record Station
    {
        /// <summary>
        /// The function of the station e.g. Pick/drop
        /// </summary>
        public Function Function { get; }

        /// <summary>
        /// The unique identifier of the station
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// The position and rotation of the station based on to the maps/graphs coordinate system
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// Type identification
        /// </summary>
        public string TypeId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="identifier"></param>
        /// <param name="position"></param>
        /// <param name="typeId"></param>
        public Station(Function function, string identifier, Position position, string typeId)
        {
            Function = function;
            Identifier = identifier;
            Position = position;
            TypeId = typeId;
        }
    }

    /// <summary>
    /// The position and rotation of the station based on to the maps/graphs coordinate system
    /// </summary>
    public record Position
    {
        /// <summary>
        /// Angle of AGV at that station
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public float Theta { get; }

        /// <summary>
        /// X coordinate
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Y coordinate
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Position(float theta, float x, float y)
        {
            Theta = theta;
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// The function of the station e.g. Pick/drop
    /// </summary>
    public enum Function
    {
        /// <summary>
        /// Pick and drop station
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        Both,

        /// <summary>
        /// Charging station
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        Charge,

        /// <summary>
        /// Drop only station
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        Drop,

        /// <summary>
        /// Park position
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        Park,

        /// <summary>
        /// Pick only station
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        Pick
    };
}
