namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// A routing key attribute to specify routing from a topic exchange to a queue in an MQTT context
/// </summary>
public class RoutingKey : Attribute
{
    /// <summary>
    /// The routing key value
    /// </summary>
    public string RoutingKeyValue { get; }

    /// <summary>
    /// Sets the routing from a topic exchange to a queue 
    /// </summary>
    /// <param name="routingKey"></param>
    public RoutingKey(string routingKey)
    {
        RoutingKeyValue = routingKey;
    }
}