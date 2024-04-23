namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines the result of the <see cref="GetMap"/> request.
/// </summary>
/// <param name="Map">The resulting map.</param>
public record GotMap(Map Map);