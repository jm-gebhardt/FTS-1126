namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

/// <summary>
/// The agv.
/// </summary>
public class Agv
{
    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The agv manufacturer.
    /// </summary>
    public string Manufacturer { get; init; }

    /// <summary>
    /// The agv series name.
    /// </summary>
    public string SeriesName { get; set; }

    /// <summary>
    /// The agv serial number.
    /// </summary>
    public string SerialNumber { get; init; }

    /// <summary>
    /// Unique Id for type of vehicle as used in the LIF.
    /// </summary>
    public string VehicleTypeId => $"{Manufacturer}.{SeriesName}";

    /// <summary>
    /// The current order of the agv.
    /// </summary>
    public Guid? CurrentOrder { get; set; }

    /// <summary>
    /// The current action id used by the fleet manager.
    /// </summary>
    public Guid? CurrentFleetManagerActionId { get; set; }

    /// <summary>
    /// Name of the interface used for mqtt communication.
    /// </summary>
    public string? InterfaceName { get; set; } 

    /// <summary>
    /// Name of the interface used for mqtt communication.
    /// </summary>
    public string? ProtocolMajorVersion { get; set; }

    /// <summary>
    /// True, if the agv has been published to the fleetmanager.
    /// </summary>
    public bool PublishedToFleetManager { get; set; }

    /// <inheritdoc cref="Agv"/>
    public Agv(Guid correlationId, string manufacturer, string seriesName, string serialNumber)
    {
        CorrelationId = correlationId;
        Manufacturer = manufacturer;
        SeriesName = seriesName;
        SerialNumber = serialNumber;
    }

    /// <summary>
    /// Set specifications for communicating with this agv.
    /// </summary>
    /// <param name="routingKey">A message routing key from an incoming message from the agv.</param>
    public void SetCommunicationSpecification(string routingKey)
    {
        //interfaceName.version.manufacturer.serialNumber.topic
        string[] parts = routingKey.Split(".");
        InterfaceName = parts[0];
        ProtocolMajorVersion = parts[1];
    }

    /// <summary>
    /// Get the routing key for this agv.
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <returns>Routing key:  interfaceName.version.manufacturer.serialNumber.topic</returns>
    public string GetRoutingKey(string topic)
    {
        return $"{InterfaceName}.{ProtocolMajorVersion}.{Manufacturer}.{SerialNumber}.{topic}";
    }

    /// <summary>
    /// Clears the agv order.
    /// </summary>
    public void ClearOrder()
    {
        CurrentOrder = null;
        CurrentFleetManagerActionId = null;
    }
}