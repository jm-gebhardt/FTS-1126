using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.Statistics.Messages.Requests;

/// <summary>
/// Represents the response to the <see cref="GetPositionFrequencies"/> request.
/// </summary>
/// <param name="PositionFrequency"></param>
public record GotPositionFrequencies(PositionFrequency PositionFrequency);