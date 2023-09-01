namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the GetAgv request which returns a list of Agvs
/// </summary>
[JetBrains.Annotations.UsedImplicitly]
public record GetAgvs(ICollection<Guid> CorrelationIds);