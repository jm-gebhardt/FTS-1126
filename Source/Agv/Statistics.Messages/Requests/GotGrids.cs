using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetGrids"/> request.
/// </summary>
/// <param name="Grids"></param>
public record GotGrids(List<Grid> Grids);