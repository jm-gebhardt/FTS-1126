using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public abstract class AgvGraphTestData
{
    public AgvGraph Graph { get; } = new(Guid.Empty,
        new []
        {
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 0f, nodeId:"Nairobi"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 42f, 80f, nodeId:"Tokyo"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, 100f, nodeId:"Marseille"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 96f, 60f, nodeId:"Berlin"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, -10f, nodeId:"Denver"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 150f, 20f, nodeId:"Helsinki"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 200f, 92f, nodeId:"Rio"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, -50f, -50f, nodeId:"Oslo"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, -100f, -100f, nodeId:"LoneDestination")
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