using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Dto with the currently assigned ActionGroup for an agv.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="ActionGroup">Can be an ActionGroup or null (if the last ActionGroup has completed).</param>
public record AssignedActionGroupDto([PublicAPI] Guid CorrelationId, ActionGroupDto? ActionGroup) : DatedMessage;