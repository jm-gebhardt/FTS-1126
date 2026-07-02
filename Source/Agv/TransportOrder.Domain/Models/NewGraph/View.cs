namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class View
{
    public string? Name { get; }

    //Area

    //IsDefault or convention that first is default?

    public IReadOnlyCollection<Node> Nodes { get; } = [];

    public IReadOnlyCollection<Edge> Edges { get; } = [];

    public IReadOnlyCollection<Station> Stations { get; } = [];
}