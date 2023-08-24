using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;

/// <summary>
/// Represents the response of the <see cref="SetAgvForceIdle"/>
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SetAgvForceIdleResult(Guid CorrelationId);