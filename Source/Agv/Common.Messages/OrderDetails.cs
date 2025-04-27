using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Utilities;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages;

/// <summary>
/// Order object used by all messages that require the order details
/// </summary>
[Serializable]
public record OrderDetails : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

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
    /// The rotation info for the orders handled box or transportation good in rad
    /// </summary>
    public double LeRotation { get; }

    /// <summary>
    /// The creator of the host order
    /// </summary>
    public HostOrderCreator Creator { get; }

    /// <summary>
    /// The UTC timestamp when the order was created
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="OrderDetails"/> with a specific creation date
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="leNo"></param>
    /// <param name="creator"></param>
    /// <param name="creationDate"></param>
    /// <param name="leRotation"></param>
    [JsonConstructor]
    public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator, DateTime creationDate, double leRotation = 0)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Source = source;
        Destination = destination;
        LeNo = leNo;
        LeRotation = leRotation;
        Creator = creator;
        CreationDate = creationDate;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OrderDetails"/> with a specific creation date
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="leNo"></param>
    /// <param name="creator"></param>
    /// <param name="creationDate"></param>
    /// <param name="huRot"></param>
    public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator, DateTime creationDate, int huRot)
        : this(correlationId, orderId, source, destination, leNo, creator, creationDate, (double)huRot)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OrderDetails"/> with creation date 'now'
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="leNo"></param>
    /// <param name="creator"></param>
    /// <param name="leRotation"></param>
    public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator, double leRotation = 0)
        : this(correlationId, orderId, source, destination, leNo, creator, DateTime.UtcNow, leRotation)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OrderDetails"/> with creation date 'now'
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="leNo"></param>
    /// <param name="creator"></param>
    /// <param name="huRot"></param>
    public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator, int huRot)
        : this(correlationId, orderId, source, destination, leNo, creator, DateTime.UtcNow, ConvertHuRotToRadians(huRot))
    {
    }

    private static double ConvertHuRotToRadians(int huRot)
    {
        return Mathematics.ConvertToRadians((huRot - 1) * 90.0);
    }
}