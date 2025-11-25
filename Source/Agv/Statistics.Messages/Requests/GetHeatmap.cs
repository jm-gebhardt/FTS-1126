namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get the heatmap for a map on a specific grid.
/// </summary>
public record GetHeatmap
{
    /// <summary>The id of the grid the position is on.</summary>
    public Guid GridId { get; init; }

    /// <summary>The CorrelationId of the Map.</summary>
    public Guid MapId { get; init; }

    /// <summary>The UTC from Date, only items with a greater Timestamp will be considered.</summary>
    public DateTime? FromDate { get; init; }

    /// <summary>The UTC to Date, only items with a less Timestamp will be considered.</summary>
    public DateTime? ToDate { get; init; }

    /// <summary>The correlation Ids of the agvs to filter for.</summary>
    public required Guid[] AgvCorrelationIds { get; init; }
}