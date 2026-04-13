using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// Provides a dto for filter templates.
/// </summary>
[PublicAPI]
public record FilterTemplate
{
    /// <summary>
    /// Name of the filter
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The type of the filter
    /// </summary>
    public FilterType FilterType { get; set; }

    /// <summary>
    /// The time filters
    /// </summary>
    public ICollection<TimeFilterTemplate> TimeFilters { get; set; } = [];

    /// <summary>
    ///  The order filter.
    /// </summary>
    public OrderFilterTemplate? OrderFilter { get; set; }

    /// <summary>
    /// The agv filter.
    /// </summary>
    public AgvFilterTemplate? AgvFilter { get; set; }
}