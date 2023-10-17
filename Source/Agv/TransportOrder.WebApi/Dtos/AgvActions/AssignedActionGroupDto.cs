using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Dto with the currently assigned ActionGroup for an Agv
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
/// <param name="ActionGroupCorrelationId">Can be an ActionGroup or null (if the last ActionGroup has completed)</param>
public record AssignedActionGroupDto([PublicAPI] Guid CorrelationId, Guid? ActionGroupCorrelationId) : DatedMessage;