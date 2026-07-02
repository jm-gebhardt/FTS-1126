namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public abstract class GraphResource
{
    public Guid Id { get; }  // unique identifier derived from LifId + resourceType + nodeId/edgeId/stationId      e.g.: ToGuid("Karis/Node/N1")  ToGuid("Still/Node/N1")?

    public string? Name { get; }

    public string? Description { get; }

    public IReadOnlyCollection<RoutingResource> Children { get; } = [];
}