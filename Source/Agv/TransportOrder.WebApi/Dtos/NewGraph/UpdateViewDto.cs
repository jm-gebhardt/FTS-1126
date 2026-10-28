using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The changes of a view. Null values stay unchanged.
/// </summary>
/// <param name="Name">The new name of the view.</param>
/// <param name="LevelId">The id of the new level the view shows.</param>
/// <param name="IsDefault">Whether the view is opened first.</param>
/// <param name="Area">The new area of the graph the view shows.</param>
[PublicAPI]
public record UpdateViewDto([MinLength(1)] string? Name, string? LevelId, bool? IsDefault, ViewAreaDto? Area);