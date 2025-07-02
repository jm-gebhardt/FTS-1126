using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.ReadModels;

/// <summary>
/// Represents the position frequencies on a grid.
/// </summary>
/// <param name="GridId">The Correlation Id of the Grid.</param>
/// <param name="MapId">The CorrelationId of the Map.</param>
/// <param name="Values">
/// The scalar values per grid cell, represented as a list of triplets: [xIndex, yIndex, value].
/// Each entry corresponds to a specific position on the grid and its associated metric (e.g., signal strength in dBm, count).
/// </param>
[UsedImplicitly]
public record ScalarHeatmap(Guid GridId, Guid MapId, ICollection<ICollection<float>> Values);