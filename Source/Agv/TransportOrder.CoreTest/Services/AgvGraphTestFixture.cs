using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;

[TestClass]
public abstract class AgvGraphTestFixture
{
    public AgvGraph Graph { get; }

    protected AgvGraphTestFixture()
    {
        const string mapName = "Map#1";
        Graph = new AgvGraph(Guid.Empty,
            new[]
            {
                new AgvGraph.Node("Saarbrücken") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                new AgvGraph.Node("Kaiserslautern") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                new AgvGraph.Node("Frankfurt") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                new AgvGraph.Node("Ludwigshafen") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                new AgvGraph.Node("Karlsruhe") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                new AgvGraph.Node("Heilbronn") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                new AgvGraph.Node("Würzburg") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f },
                new AgvGraph.Node("LoneRoute") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = -50f, Y = -50f, Z = 0f },
                new AgvGraph.Node("LoneDestination") { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = -100f, Y = -100f, Z = 0f }
            },
            new[]
            {
                new AgvGraph.Edge("Saarbrücken", "Kaiserslautern") { Metric = 70f },
                new AgvGraph.Edge("Saarbrücken", "Karlsruhe") { Metric = 145f },
                new AgvGraph.Edge("Kaiserslautern", "Frankfurt") { Metric = 103f},
                new AgvGraph.Edge("Kaiserslautern", "Ludwigshafen") { Metric = 53f},
                new AgvGraph.Edge("Karlsruhe", "Heilbronn") { Metric = 84f },
                new AgvGraph.Edge("Frankfurt", "Würzburg") { Metric = 116f },
                new AgvGraph.Edge("Ludwigshafen", "Würzburg") { Metric = 183f },
                new AgvGraph.Edge("Heilbronn", "Würzburg") { Metric = 102f },
                new AgvGraph.Edge("Saarbrücken", "LoneRoute") { Metric = 50f },
                new AgvGraph.Edge("LoneRoute", "LoneDestination") { Metric = 60f}
            },
            mapName.ToGuid(),
            mapName);
    }

    public abstract string[][] GetAllPossibleRoutesOrdered();

    public void BlockEdgeInRoute(string[] route)
    {
        int routeLength = route.Length;
        Graph.Edges.First(edge => edge.From == route[routeLength - 2] && edge.To == route[routeLength - 1]).IsBlocked = true;
    }
}