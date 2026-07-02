namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class Graph
{
    public IReadOnlyCollection<Node> Nodes { get; } = [];

    public IReadOnlyCollection<Edge> Edges { get; } = [];

    public IReadOnlyCollection<Station> Stations { get; } = [];



    
}