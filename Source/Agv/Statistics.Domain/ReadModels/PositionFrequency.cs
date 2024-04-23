using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.ReadModels;

/// <summary>
/// Represents the position frequencies on a grid.
/// </summary>
/// <param name="GridId">The Correlation Id of the Grid.</param>
/// <param name="MapId">The CorrelationId of the Map.</param>
/// <param name="Frequencies">The position frequencies [[xIndex, yIndex, value]].</param>
[UsedImplicitly]
public record PositionFrequency(Guid GridId, Guid MapId, ICollection<ICollection<float>> Frequencies);