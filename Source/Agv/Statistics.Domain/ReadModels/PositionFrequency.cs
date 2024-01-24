using JetBrains.Annotations;

namespace Statistics.Domain.ReadModels;

/// <summary>
/// Represents the position frequencies on a grid.
/// </summary>
/// <param name="GridId">The grid id.</param>
/// <param name="Frequencies">The position frequencies [[xIndex, yIndex, value]].</param>
[UsedImplicitly]
public record PositionFrequency(Guid GridId, ICollection<ICollection<float>>Frequencies);