using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to collect error occurrences.
/// </summary>
/// <param name="Error">The Agv error to be added.</param>
/// <param name="GridPositionIds">The id of the agv grid positions.</param>
public record AddError(AgvError Error, List<Guid> GridPositionIds);
