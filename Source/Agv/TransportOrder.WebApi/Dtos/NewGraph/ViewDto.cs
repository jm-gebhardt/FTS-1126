using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A view on the graph. One level, one area and one background image.
/// </summary>
/// <param name="Id">The id of the view.</param>
/// <param name="Name">The name of the view.</param>
/// <param name="LevelId">The id of the level the view shows.</param>
/// <param name="IsDefault">Whether the view is opened first.</param>
/// <param name="IsGenerated">Whether the view is generated from the graph and not stored.</param>
/// <param name="Area">The area of the graph the view shows.</param>
/// <param name="Background">The placement of the background image. Null if the view has none.</param>
[PublicAPI]
public record ViewDto(Guid Id, string Name, string? LevelId, bool IsDefault, bool IsGenerated, ViewAreaDto Area, ViewBackgroundDto? Background);