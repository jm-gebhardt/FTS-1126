namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public abstract class RoutingResource
{
    public Guid Id { get; }  // unique identifier defined on creation

    public AgvType AgvType { get; } = null!;

    //public Guid ParentId { get; } needed for DB?

    public GraphResource Parent { get; } = null!;

    public IReadOnlyCollection<RoutingResource> ConflictingResources { get; } = [];
}