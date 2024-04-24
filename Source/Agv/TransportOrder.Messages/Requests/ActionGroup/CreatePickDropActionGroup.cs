using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to create a pick-drop action group.
/// </summary>
/// <param name="Source">The source node.</param>
/// <param name="Target">The target node.</param>
/// <param name="AssignedAgv">The assigned agv (optional).</param>
/// <param name="HasPriority">True, if the created action group is supposed to have priority (to be started immediately)</param>
/// <param name="Creator">The creator of the action group.</param>
public record CreatePickDropActionGroup(string Source, string Target, Guid? AssignedAgv, bool HasPriority, ActionGroupCreator Creator);