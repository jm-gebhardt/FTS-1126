using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

[PublicAPI]
public static class NewGraphGenerator
{
    public const string LevelId = "level";
    public const string StartNodeName = "start";
    public const string EndNodeName = "end";
    public const string UnknownNodeName = "unknown";
    public const string EdgeName = "start-end";
    public const string StationName = "station";

    public static Graph GenerateGraph(Guid? stationInteractionNodeId = null)
    {
        var startNode = new Node(Guid.NewGuid(), StartNodeName, null, 0, 0, LevelId);
        var endNode = new Node(Guid.NewGuid(), EndNodeName, null, 1, 1, LevelId);
        var edge = new Edge(Guid.NewGuid(), EdgeName, null, startNode.Id, endNode.Id);
        var interactionNode = stationInteractionNodeId is null ? endNode : new Node(stationInteractionNodeId.Value, UnknownNodeName, null, 2, 2, LevelId);
        var station = new Station(Guid.NewGuid(), StationName, null, 0.5, 1, 1, 0) { InteractionNodes = [interactionNode] };
        List<Node> nodes = [startNode, endNode];
        List<Edge> edges = [edge];
        List<Station> stations = [station];

        return new Graph(nodes, edges, stations);
    }
}