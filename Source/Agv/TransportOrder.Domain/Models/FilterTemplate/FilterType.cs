#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// Filter type
/// </summary>
[PublicAPI]
public enum FilterType
{
    Order,
    Agv,
    Station,
    LogEntry
}