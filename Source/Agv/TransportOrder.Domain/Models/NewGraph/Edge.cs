namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class Edge : GraphResource
{
    public Node StartNode { get; } = null!;

    public Node EndNode { get; } = null!;
}