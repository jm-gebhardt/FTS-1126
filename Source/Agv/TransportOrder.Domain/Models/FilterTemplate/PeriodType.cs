#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
///  Time period type.
/// </summary>
[PublicAPI]
public enum PeriodType
{
    Today,
    LastWeek,
    LastMonth,
    ThisYear,
    LastYear,
    Range
}