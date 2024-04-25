using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// Provides a command to collect error occurrences.
/// </summary>
/// <param name="Error">The error.</param>
/// <param name="PositionId">The id of the agv grid position.</param>
public record AddError(AgvError Error, Guid PositionId);