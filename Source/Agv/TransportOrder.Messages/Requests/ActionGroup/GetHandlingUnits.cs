namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents a request to retrieve distinct handling units matching the query.
/// </summary>
/// <param name="Query">The substring to search for (case-insensitive).</param>
/// <param name="Limit">The maximum number of handling unit results to return.</param>
public record GetHandlingUnits(string Query, int Limit = 100);