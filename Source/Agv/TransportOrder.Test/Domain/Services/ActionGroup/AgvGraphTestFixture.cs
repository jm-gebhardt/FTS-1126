using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public abstract class AgvGraphTestData
{
    public AgvGraph Graph { get; } = AgvGraphGenerator.Generate(
        [
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 0f, 0f, nodeName: "Nairobi"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 42f, 80f, nodeName: "Tokyo"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, 100f, nodeName: "Marseille"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 96f, 60f, nodeName: "Berlin"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 104f, -10f, nodeName: "Denver"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 150f, 20f, nodeName: "Helsinki"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, 200f, 92f, nodeName: "Rio"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, -50f, -50f, nodeName: "Oslo"),
            NodeGenerator.GenerateNode(AgvGraph.NodeType.PickDrop, -100f, -100f, nodeName: "LoneDestination")
        ],
        new[]
        {
            GraphEdge.CreateDirect("Nairobi".ToGuid(), "Tokyo".ToGuid(), 70f),
            GraphEdge.CreateDirect("Nairobi".ToGuid(), "Denver".ToGuid(), 145f),
            GraphEdge.CreateDirect("Tokyo".ToGuid(), "Marseille".ToGuid(), 103f),
            GraphEdge.CreateDirect("Tokyo".ToGuid(), "Berlin".ToGuid(), 53f),
            GraphEdge.CreateDirect("Denver".ToGuid(), "Helsinki".ToGuid(), 84f),
            GraphEdge.CreateDirect("Marseille".ToGuid(), "Rio".ToGuid(), 116f),
            GraphEdge.CreateDirect("Berlin".ToGuid(), "Rio".ToGuid(), 183f),
            GraphEdge.CreateDirect("Helsinki".ToGuid(), "Rio".ToGuid(), 102f),
            GraphEdge.CreateDirect("Nairobi".ToGuid(), "Oslo".ToGuid(), 50f),
            GraphEdge.CreateDirect("Oslo".ToGuid(), "LoneDestination".ToGuid(), 60f)
        });

    public abstract string[][] GetAllPossibleRoutesOrdered();
}