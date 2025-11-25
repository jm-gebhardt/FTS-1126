using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to add a new grid.
/// </summary>
/// <param name="Name">The grid name.</param>
/// <param name="XStep">The x resolution (cell width).</param>
/// <param name="YStep">The y resolution (cell height).</param>
[PublicAPI]
public record AddGrid(string Name, decimal XStep, decimal YStep);