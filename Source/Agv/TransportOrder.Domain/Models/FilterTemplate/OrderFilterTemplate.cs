using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// A filter template for order filters.
/// </summary>
[PublicAPI]
public record OrderFilterTemplate
{
    /// <summary>
    /// The handling units of orders.
    /// </summary>
    public ICollection<string>? HandlingUnits { get; set; }

    /// <summary>
    /// The order types.
    /// </summary>
    public ICollection<OrderType>? OrderTypes { get; set; }

    /// <summary>
    /// The order paths.
    /// </summary>
    public ICollection<OrderPath>? Paths { get; set; }

    /// <summary>
    /// The order duration ranges.
    /// </summary>
    public ICollection<Duration>? DurationRanges { get; set; }

    /// <summary>
    /// The order status.
    /// </summary>
    public ICollection<OrderStatus>? Status { get; set; }

    /// <summary>
    /// The incident types.
    /// </summary>
    public ICollection<IncidentType>? IncidentTypes { get; set; }
}