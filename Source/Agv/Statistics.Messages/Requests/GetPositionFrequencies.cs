namespace Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get the position frequency for a map on a specific grid. 
/// </summary>
/// <param name="GridId">The id of the grid the position is on.</param>
/// <param name="MapId">The CorrelationId of the Map.</param>
/// <param name="FromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
/// <param name="ToDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
public record GetPositionFrequencies(Guid GridId, Guid MapId, DateTime? FromDate, DateTime? ToDate);