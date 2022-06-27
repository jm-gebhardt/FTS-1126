using Gebhardt.StoreWare.Agv.Common.Definitions;

namespace Gebhardt.StoreWare.Agv.Common.Messages;


/// <summary>
/// Order object used by all messages that require the order details
/// </summary>
[Serializable]
public class Order
{
    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// The source where to pick up the order
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// The destination where to drop the order
    /// </summary>
    public string Destination { get; }

    /// <summary>
    /// The LeNo of the orders handled box or transportation good
    /// </summary>
    public string LeNo { get; }

    /// <summary>
    /// The creator of the host order
    /// </summary>
    public HostOrderCreator Creator { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="leNo"></param>
    /// <param name="creator"></param>
    public Order(string orderId, string source, string destination, string leNo, HostOrderCreator creator)
    {
        OrderId = orderId;
        Source = source;
        Destination = destination;
        LeNo = leNo;
        Creator = creator;
    }
}
