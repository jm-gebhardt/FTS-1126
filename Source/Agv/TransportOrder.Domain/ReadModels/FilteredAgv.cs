using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model of a filtered agv.
/// </summary>
[PublicAPI]
public record FilteredAgv
{
    /// <inheritdoc cref="Agv.CorrelationId"/>
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="Agv.SerialNumber"/>
    public string SerialNumber { get; init; } = null!;

    /// <inheritdoc cref="Agv.Name"/>
    public string Name { get; private set; } = null!;

    /// <inheritdoc cref="Agv.AgvStatus"/>
    public AgvStatus? AgvStatus { get; set; }

    /// <inheritdoc cref="Agv.Mode"/>
    public AgvMode Mode { get; init; } = null!;
}