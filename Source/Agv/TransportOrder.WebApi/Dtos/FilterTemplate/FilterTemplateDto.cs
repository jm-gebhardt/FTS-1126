using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterTemplate;

/// <summary>
/// Provides a dto for filter templates.
/// </summary>
[PublicAPI]
public record FilterTemplateDto
{
    /// <summary>
    /// The filter id.
    /// </summary>
    public Guid Id { get; set; }

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
    public ICollection<TimeFilterTemplateDto> TimeFilters { get; set; } = [];

    /// <summary>
    ///  The order filter.
    /// </summary>
    public OrderFilterTemplateDto? OrderFilter { get; set; }

    /// <summary>
    /// The agv filter.
    /// </summary>
    public AgvFilterTemplateDto? AgvFilter { get; set; }
}