using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

[TestClass]
public abstract class AgvGraphTestFixture
{
    public AgvGraph Graph { get; } = new(Guid.Empty,
        new GraphNode[]
        {
            new("Nairobi") {Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
            new("Tokyo") {Type = AgvGraph.NodeType.PickDrop, X = 42f, Y = 80f, Z = 0f},
            new("Marseille") {Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = 100f, Z = 0f},
            new("Berlin") {Type = AgvGraph.NodeType.PickDrop, X = 96f, Y = 60f, Z = 0f},
            new("Denver") {Type = AgvGraph.NodeType.PickDrop, X = 104f, Y = -10f, Z = 0f},
            new("Helsinki") {Type = AgvGraph.NodeType.PickDrop, X = 150f, Y = 20f, Z = 0f},
            new("Rio") {Type = AgvGraph.NodeType.PickDrop, X = 200f, Y = 92f, Z = 0f},
            new("Oslo") {Type = AgvGraph.NodeType.PickDrop, X = -50f, Y = -50f, Z = 0f},
            new("LoneDestination") {Type = AgvGraph.NodeType.PickDrop, X = -100f, Y = -100f, Z = 0f}
        },
        new[]
        {
            GraphEdge.CreateDirect("Nairobi", "Tokyo", 70f),
            GraphEdge.CreateDirect("Nairobi", "Denver", 145f),
            GraphEdge.CreateDirect("Tokyo", "Marseille", 103f),
            GraphEdge.CreateDirect("Tokyo", "Berlin", 53f),
            GraphEdge.CreateDirect("Denver", "Helsinki", 84f),
            GraphEdge.CreateDirect("Marseille", "Rio", 116f),
            GraphEdge.CreateDirect("Berlin", "Rio", 183f),
            GraphEdge.CreateDirect("Helsinki", "Rio", 102f),
            GraphEdge.CreateDirect("Nairobi", "Oslo", 50f),
            GraphEdge.CreateDirect("Oslo", "LoneDestination", 60f)
        });

    public abstract string[][] GetAllPossibleRoutesOrdered();
}