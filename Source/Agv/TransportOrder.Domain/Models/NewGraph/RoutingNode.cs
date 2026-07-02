namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class RoutingNode : RoutingResource
{
    public double Theta { get; }

    //public Node ParentNode { get; } = null!; computed property?

    public LoadState LoadState { get; } = null!;

    // actions? -> feedback from Andreas
}