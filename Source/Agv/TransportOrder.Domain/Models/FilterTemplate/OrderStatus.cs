#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// The order status.
/// </summary>
[PublicAPI]
public enum OrderStatus
{
    Completed,
    Failed,
    Paused
}