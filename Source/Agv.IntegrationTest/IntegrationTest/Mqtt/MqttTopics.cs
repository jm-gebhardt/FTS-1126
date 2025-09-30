namespace Gebhardt.Acc.IntegrationTest.Mqtt;

/// <summary>
/// Mqtt Topics
/// </summary>
public static class MqttTopics
{
    /// <summary>
    /// Agv Status
    /// </summary>
    public const string Status = "agv/status";

    /// <summary>
    /// Agv Data
    /// </summary>
    public const string AgvData = "agv/agvData";

    /// <summary>
    /// Agv Graph
    /// </summary>
    public const string Graph = "agv/graph";

    /// <summary>
    /// Agv StationList
    /// </summary>
    public const string StationList = "agv/stationList";

    /// <summary>
    /// Agv Map
    /// </summary>
    public const string Map = "agv/map";

    /// <summary>
    /// Order event
    /// </summary>
    public const string OrderEvent = "agv/orderEvent";

    /// <summary>
    /// Data requested
    /// </summary>
    public const string RequestData = "agv/requestAgvData";

    /// <summary>
    /// Graph requested
    /// </summary>
    public const string RequestGraph = "agv/requestGraph";

    /// <summary>
    /// StationList requested
    /// </summary>
    public const string RequestStationList = "agv/requestStationList";

    /// <summary>
    /// Map requested
    /// </summary>
    public const string RequestMap = "agv/requestMap";

    /// <summary>
    /// Order requested
    /// </summary>
    public const string RequestOrder = "agv/order";
}