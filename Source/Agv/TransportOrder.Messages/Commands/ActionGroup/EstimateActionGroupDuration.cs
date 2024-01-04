using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Command to estimate the duration of an ActionGroup.
/// </summary>
/// <param name="FirstAction">The first Action of the ActionGroup.</param>
/// <param name="FirstActionCosts">The costs of the first Action of the ActionGroup.</param>
/// <param name="AgvId">The correlationId of the assigned Agv.</param>
/// <param name="ActionGroupId">The correlationId of the ActionGroup.</param>
public record EstimateActionGroupDuration(AgvAction FirstAction, double FirstActionCosts, Guid AgvId, Guid ActionGroupId);