using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Represents a command for tracking the stay of an AGV in a specific grid cell.
/// </summary>
/// <param name="GridCellStay">The GridCellStay.</param>
public record AddGridCellStay(GridCellStay GridCellStay);