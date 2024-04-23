namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to create a pick-drop action group.
/// </summary>
/// <param name="Source">The source node.</param>
/// <param name="Target">The target node.</param>
/// <param name="AssignedAgv">The assigned agv (optional).</param>
public record CreatePickDropActionGroup(string Source, string Target, Guid? AssignedAgv);