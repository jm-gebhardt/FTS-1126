using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetPositionFrequencies"/> request.
/// </summary>
/// <param name="PositionFrequency"></param>
public record GotPositionFrequencies(ScalarHeatmap PositionFrequency) : GotHeatmap(PositionFrequency);