using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterTemplate;

/// <summary>
/// A filter template for agv filters.
/// </summary>
[PublicAPI]
public record AgvFilterTemplateDto
{
    /// <summary>
    /// Serial numbers of agvs
    /// </summary>
    public ICollection<string> SerialNumbers { get; set; } = [];
}