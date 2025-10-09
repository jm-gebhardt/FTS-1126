using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Domain read model of a filtered agv.
/// </summary>
[PublicAPI]
public record FilteredAgv
{
    /// <summary>
    /// Correlation id of this agv.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Serial number of the agv.
    /// </summary>
    public string SerialNumber { get; init; } = null!;

    /// <summary>
    /// The name of the agv.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Last received agv status.
    /// </summary>
    public AgvStatus? AgvStatus { get; set; }

    /// <summary>
    /// The mode of the agv.
    /// </summary>
    public AgvMode Mode { get; init; } = null!;
}