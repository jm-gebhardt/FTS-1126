using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get the error frequency for a map on a specific grid. 
/// </summary>
public record GetErrorFrequencies : GetHeatmap
{
    /// <summary>The error level.</summary>
    public ErrorLevel ErrorLevel { get; init; }

    /// <summary>The error code.</summary>
    public string? ErrorCode { get; init; }
}