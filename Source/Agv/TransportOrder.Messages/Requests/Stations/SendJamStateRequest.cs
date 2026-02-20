namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request for the JamState of a station.
/// </summary>
/// <param name="CorrelationId">The station to send the JamState request to.</param>
/// <param name="User">The user that requested the JamState request.</param>
public record SendJamStateRequest(Guid CorrelationId, string User);