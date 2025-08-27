namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to the command <see cref="GetHandlingUnits"/>.
/// </summary>
/// <param name="HandlingUnits">The list of handling unit identifiers.</param>
public record GotHandlingUnits(List<string> HandlingUnits);