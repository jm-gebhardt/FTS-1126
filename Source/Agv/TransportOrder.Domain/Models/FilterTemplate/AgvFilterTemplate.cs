using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

/// <summary>
/// A filter template for agv filters.
/// </summary>
[PublicAPI]
public record AgvFilterTemplate
{
    /// <summary>
    /// Serial numbers of agvs
    /// </summary>
    public ICollection<string> SerialNumbers { get; set; } = [];
}