using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetStayDurations"/> request.
/// </summary>
/// <param name="PositionFrequency"></param>
public record GotStayDurations(ScalarHeatmap PositionFrequency) : GotHeatmap(PositionFrequency);