namespace Gebhardt.Acc.PrismatCommunication.Messages.Events;

/// <summary>
///  Represents the event raised when an agv left a station group.
/// </summary>
/// <param name="Source">The station source.</param>
/// <param name="Target">The station target.</param>
public record AgvLeftStation(string Source, string Target);