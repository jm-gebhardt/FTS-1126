namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class Station : GraphResource
{
    public List<Node> InteractionNodes { get; } = [];

    public double? StationHeight { get; }

    public double X { get; }

    public double Y { get; }

    public double? Theta { get; }
}