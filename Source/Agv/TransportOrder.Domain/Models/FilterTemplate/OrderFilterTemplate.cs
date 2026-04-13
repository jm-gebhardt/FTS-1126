using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// A filter template for order filters.
/// </summary>
[PublicAPI]
public record OrderFilterTemplate
{
    /// <summary>
    /// The handling unit of the order.
    /// </summary>
    public string? HandlingUnit { get; set; }

    /// <summary>
    /// The order type.
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// The order path.
    /// </summary>
    public OrderPath? Path { get; set; }

    /// <summary>
    /// The order duration range.
    /// </summary>
    public Duration? DurationRange { get; set; }

    /// <summary>
    /// The order status.
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// The incident type.
    /// </summary>
    public IncidentType? IncidentType { get; set; }
}