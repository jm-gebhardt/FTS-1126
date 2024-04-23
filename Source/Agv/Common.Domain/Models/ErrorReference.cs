using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// Object that holds the error reference (e.g. orderId, orderUpdateId, actionId...) as
/// key-value pairs.
/// </summary>
[PublicAPI]
public record ErrorReference
{
    /// <summary>
    /// Property for reference key
    /// </summary>
    public string? ReferenceKey { get; set; }

    /// <summary>
    /// Property for reference value
    /// </summary>
    public string? ReferenceValue { get; set; }
}