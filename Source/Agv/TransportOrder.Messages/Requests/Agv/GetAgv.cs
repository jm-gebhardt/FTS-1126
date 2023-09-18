using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the request which returns the AgvDto
/// </summary>
[UsedImplicitly]
public record GetAgv(Guid CorrelationId);