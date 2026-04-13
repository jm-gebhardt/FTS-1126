using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// The path of the order.
/// </summary>
[PublicAPI]
public record OrderPath
{
    /// <summary>
    /// The source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The source.
    /// </summary>
    public string? Target { get; set; }
}