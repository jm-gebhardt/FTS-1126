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
                new("Nairobi") { Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                new("Tokyo") { Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f },
                new("Marseille") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f },
                new("Berlin") { Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f },
                new("Denver") { Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f },
                new("Helsinki") { Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f },
                new("Rio") { Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f },
                new("Oslo") { Type = AgvGraph.NodeType.PickDrop, X = -50f, Y = -50f, Z = 0f },
                new("LoneDestination") { Type = AgvGraph.NodeType.PickDrop, X = -100f, Y = -100f, Z = 0f }
            },
            new GraphEdge[]
            {
                new("Nairobi", "Tokyo", 70f),
                new("Nairobi", "Denver", 145f),
                new("Tokyo", "Marseille", 103f),
                new("Tokyo", "Berlin", 53f),
                new("Denver", "Helsinki", 84f),
                new("Marseille", "Rio", 116f),
                new("Berlin", "Rio", 183f),
                new("Helsinki", "Rio", 102f),
                new("Nairobi", "Oslo", 50f),
                new("Oslo", "LoneDestination", 60f)
            },
            mapName.ToGuid(),
            mapName);
    }

    public abstract string[][] GetAllPossibleRoutesOrdered();
}