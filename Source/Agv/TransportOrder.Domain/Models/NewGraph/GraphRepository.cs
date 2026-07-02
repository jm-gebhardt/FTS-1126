namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

public class GraphRepository
{
    // methods to create, update, remove and query the graph resources. for example:

    public Node? GetNodeById(Guid nodeId)
    {
        throw new InvalidOperationException();
    }

    public Node? GetNodeByName(string nodeName)
    {
        throw new InvalidOperationException();
    }

    public Station[] GetChargingStations()
    {
        throw new InvalidOperationException();
    }

    public string[] GetLevelIds()
    {
        throw new InvalidOperationException();
    }

    public (Node[] Nodes, Edge[] Edges, Station[] Stations) GetLevel(string levelId)
    {
        throw new InvalidOperationException();
    }
}