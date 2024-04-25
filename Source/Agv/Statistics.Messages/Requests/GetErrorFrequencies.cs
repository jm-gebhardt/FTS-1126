using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get the error frequency for a map on a specific grid. 
/// </summary>
/// <param name="GridId">The id of the grid the position is on.</param>
/// <param name="MapId">The CorrelationId of the Map.</param>
/// <param name="FromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
/// <param name="ToDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
/// <param name="AgvSerialNumbers">The serial numbers of the agvs that were in error state.</param>
/// <param name="ErrorLevel">The error level.</param>
/// <param name="ErrorCode">The error code.</param>
public record GetErrorFrequencies(Guid GridId, Guid MapId, DateTime? FromDate, DateTime? ToDate, string[] AgvSerialNumbers, ErrorLevel ErrorLevel, string? ErrorCode);