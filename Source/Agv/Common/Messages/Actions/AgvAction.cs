using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions;

/// <summary>
/// Agv Action
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AgvAction : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <summary>
    /// The CorrelationId of the corresponding order
    /// </summary>
    public Guid OrderCorrelationId { get; }

    /// <summary>
    /// The CorrelationId of the assigned agv
    /// </summary>
    public Guid AgvCorrelationId { get; set; }

    /// <summary>
    /// The type of the order, e.g. Pick/Drop/Transport
    /// </summary>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The details of the corresponding order
    /// </summary>
    public OrderDetails OrderDetails { get; set; }

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public string[] Nodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// After that date the action is allowed to throw its failure without any further retry
    /// </summary>
    public TimeSpan ExpirationDate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="correlationId">CorrelationId</param>
    /// <param name="actionType">Type of the order, e.g. Pick/Drop/Transport</param>
    /// <param name="orderDetails">Details of the corresponding order</param>
    public AgvAction(Guid correlationId, ActionType actionType, OrderDetails orderDetails)
    {
        CorrelationId = correlationId;
        OrderCorrelationId = orderDetails.CorrelationId;
        ActionType = actionType;
        OrderDetails = orderDetails;
    }

}