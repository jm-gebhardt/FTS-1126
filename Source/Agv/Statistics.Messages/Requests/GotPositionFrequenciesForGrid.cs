using Statistics.Domain.ReadModels;

namespace Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetPositionFrequenciesForGrid"/> request.
/// </summary>
/// <param name="PositionFrequency"></param>
public record GotPositionFrequenciesForGrid(PositionFrequency PositionFrequency);