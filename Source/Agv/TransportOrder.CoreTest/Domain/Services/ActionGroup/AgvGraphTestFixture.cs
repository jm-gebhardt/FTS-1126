using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

[TestClass]
public abstract class AgvGraphTestFixture
{
    public AgvGraph Graph { get; }

    protected AgvGraphTestFixture()
    {
        const string mapName = "Map#1";
        Graph = new AgvGraph(Guid.Empty,
            new GraphNode[]
            {
                new("Saarbrücken") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                new("Kaiserslautern") { Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                new("Frankfurt") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                new("Ludwigshafen") { Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                new("Karlsruhe") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                new("Heilbronn") { Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                new("Würzburg") { Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f },
                new("LoneRoute") { Type = AgvGraph.NodeType.PickDrop, X = -50f, Y = -50f, Z = 0f },
                new("LoneDestination") { Type = AgvGraph.NodeType.PickDrop, X = -100f, Y = -100f, Z = 0f }
            },
            new GraphEdge[]
            {
                new("Saarbrücken", "Kaiserslautern", 70f),
                new("Saarbrücken", "Karlsruhe", 145f),
                new("Kaiserslautern", "Frankfurt", 103f),
                new("Kaiserslautern", "Ludwigshafen", 53f),
                new("Karlsruhe", "Heilbronn", 84f),
                new("Frankfurt", "Würzburg", 116f),
                new("Ludwigshafen", "Würzburg", 183f),
                new("Heilbronn", "Würzburg", 102f),
                new("Saarbrücken", "LoneRoute", 50f),
                new("LoneRoute", "LoneDestination", 60f)
            },
            mapName.ToGuid(),
            mapName);
    }

    public abstract string[][] GetAllPossibleRoutesOrdered();
}