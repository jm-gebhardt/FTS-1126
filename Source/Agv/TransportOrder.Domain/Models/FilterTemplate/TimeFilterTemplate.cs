using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// Provides a dto for time filter templates.
/// </summary>
[PublicAPI]
public record TimeFilterTemplate
{
    /// <summary>
    /// Date type.
    /// </summary>
    public SortBy DateType { get; set; }

    /// <summary>
    /// Time period type.
    /// </summary>
    public PeriodType PeriodType { get; set; }

    /// <summary>
    /// Start date of the time period (if period type is 'Range').
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// End date of the time period (if period type is 'Range').
    /// </summary>
    public DateTime? To { get; set; }
}