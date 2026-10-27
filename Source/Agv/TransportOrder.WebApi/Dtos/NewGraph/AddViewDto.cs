using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// The data of a view to create.
/// </summary>
/// <param name="Name">The name of the view.</param>
/// <param name="LevelId">The id of the level the view shows.</param>
/// <param name="IsDefault">Whether the view is opened first.</param>
/// <param name="Area">The area of the graph the view shows.</param>
[PublicAPI]
public record AddViewDto([MinLength(1)] string Name, string? LevelId, bool IsDefault, ViewAreaDto Area);