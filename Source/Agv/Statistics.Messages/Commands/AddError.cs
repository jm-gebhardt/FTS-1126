using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to collect error occurrences.
/// </summary>
/// <param name="Error">The Agv error to be added.</param>
/// <param name="GridCellIds">The id of the grid cells.</param>
public record AddError(AgvError Error, List<Guid> GridCellIds);