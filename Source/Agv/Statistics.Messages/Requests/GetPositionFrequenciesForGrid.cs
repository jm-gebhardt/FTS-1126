namespace Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get the position frequency for a grid.
/// </summary>
/// <param name="GridId"></param>
public record GetPositionFrequenciesForGrid(Guid GridId);