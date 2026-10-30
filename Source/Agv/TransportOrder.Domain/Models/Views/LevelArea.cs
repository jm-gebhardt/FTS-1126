using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Views;

/// <summary>
/// The area covered by the nodes and stations of one level.
/// </summary>
/// <param name="LevelId">The id of the level.</param>
/// <param name="Area">The smallest area containing all nodes and stations of the level.</param>
[PublicAPI]
public record LevelArea(string? LevelId, ViewArea Area);