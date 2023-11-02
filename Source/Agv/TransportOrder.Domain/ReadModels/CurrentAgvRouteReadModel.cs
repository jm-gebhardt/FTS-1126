using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

/// <summary>
/// The current route of the Agv. It is null when the Agv has completed the last action.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv.</param>
/// <param name="RemainingRoute">The remaining route in the form of nodes and edges.</param>
public record CurrentAgvRouteReadModel([UsedImplicitly] Guid CorrelationId, string[] RemainingRoute);