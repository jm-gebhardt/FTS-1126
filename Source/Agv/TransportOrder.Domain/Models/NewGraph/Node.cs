namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class Node : GraphResource
{
    public double X { get; }

    public double Y { get; }

    public string? LevelId { get; }

    public IReadOnlyCollection<Edge> IncomingEdges { get; } = [];

    public IReadOnlyCollection<Edge> OutgoingEdges { get; } = [];
}