namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Provides a request to get backgrounds, for given maps, if provided.
/// </summary>
/// <param name="MapIds">Optional, ids of maps the backgrounds are on.</param>
/// <param name="MaxCount">Maximum number of backgrounds returned.</param>
public record GetBackgrounds(ICollection<Guid> MapIds, int MaxCount=50);