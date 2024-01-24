using Statistics.Domain.Models;

namespace Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetGrids"/> request.
/// </summary>
/// <param name="Grids"></param>
public record GotGrids(List<Grid> Grids);