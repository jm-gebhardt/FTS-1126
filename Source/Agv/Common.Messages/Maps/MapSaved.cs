namespace Gebhardt.Acc.Common.Messages.Maps;

/// <summary>
/// Provides an event after map is saved.
/// </summary>
/// <param name="Map">The map.</param>
public record MapSaved(Common.Domain.Models.Graphs.Map Map);