using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to create a pick-drop action group.
/// </summary>
/// <param name="Source">The source node.</param>
/// <param name="Target">The target node.</param>
/// <param name="DockFrontPick">Whether to dock front for the pick.</param>
/// <param name="DockFrontDrop">Whether to dock front for the drop.</param>
/// <param name="HandlingUnit">The handling unit(optional).</param>
/// <param name="AssignedAgv">The assigned agv (optional).</param>
/// <param name="AdditionalPickInfo">Additional information to attach to the pick(optional).</param>
/// <param name="AdditionalDropInfo">Additional information to attach to the drop(optional).</param>
/// <param name="HasPriority">True, if the created action group is supposed to have priority (to be started immediately)</param>
/// <param name="Creator">The creator of the action group.</param>
/// <param name="User">The user who created the action group.</param>
public record CreatePickDropActionGroup(string? Source, string? Target, bool DockFrontPick, bool DockFrontDrop, string? HandlingUnit, Guid? AssignedAgv, string? AdditionalPickInfo, string? AdditionalDropInfo, bool HasPriority, ActionGroupCreator Creator, string User);