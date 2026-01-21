using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Contains the latest action groups for an AGV by state.
/// </summary>
/// <param name="Created">The latest created action group, if any.</param>
/// <param name="Started">The latest started action group, if any.</param>
/// <param name="Completed">The latest completed action group, if any.</param>
public record AgvActionGroupsByState(ActionGroup? Created, ActionGroup? Started, ActionGroup? Completed);