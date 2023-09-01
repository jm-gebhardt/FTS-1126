namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the request which returns the AgvDto
/// </summary>
[JetBrains.Annotations.UsedImplicitly]
public record GetAgv(Guid CorrelationId);