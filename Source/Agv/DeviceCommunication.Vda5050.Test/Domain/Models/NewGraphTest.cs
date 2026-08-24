using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models;

[TestClass]
public class NewGraphTest
{
    [TestMethod]
    public void ConvertToGraph_ValidData_CorrectlyConverted()
    {
        var layoutCollection = NewGraphGenerator.SampleCollection;

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        graph.Edges.Count.ShouldBe(1);
        graph.Nodes.Count.ShouldBe(2);
        graph.Stations.Count.ShouldBe(1);
    }

    [TestMethod]
    public void ConvertToGraph_SubsetOfLayouts_OnlyConvertsTheGivenLayouts()
    {
        var layout = NewGraphGenerator.GenerateLayout();
        var layoutCollection = NewGraphGenerator.GenerateCollection(layout, NewGraphGenerator.GenerateLayout(NewGraphGenerator.OtherLayoutId));

        var graph = layoutCollection.ToGraph([layout]);

        graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.EndNodeId)
        ], true);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_EdgeAndStationReferenceTheNodesOfTheLayout()
    {
        var layoutCollection = NewGraphGenerator.GenerateCollection(NewGraphGenerator.GenerateLayout());

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        var startNodeId = NewGraphGenerator.GenerateInternalId(NewGraphGenerator.StartNodeId);
        var endNodeId = NewGraphGenerator.GenerateInternalId(NewGraphGenerator.EndNodeId);
        var edge = graph.Edges.ShouldHaveSingleItem();
        edge.Id.ShouldBe(NewGraphGenerator.GenerateInternalId(NewGraphGenerator.EdgeId));
        edge.StartNodeId.ShouldBe(startNodeId);
        edge.EndNodeId.ShouldBe(endNodeId);
        var station = graph.Stations.ShouldHaveSingleItem();
        station.Id.ShouldBe(NewGraphGenerator.GenerateInternalId(NewGraphGenerator.StationId));
        station.InteractionNodes.ShouldHaveSingleItem().Id.ShouldBe(startNodeId);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_TakesNodeLevelIdFromLayout()
    {
        var layoutCollection = NewGraphGenerator.GenerateCollection(NewGraphGenerator.GenerateLayout());

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        graph.Nodes.ShouldAllBe(n => n.LevelId == NewGraphGenerator.LevelId);
    }

    [TestMethod]
    public void ConvertToGraph_MultipleLayouts_MergesEveryLayoutIntoOneGraph()
    {
        var layoutCollection = NewGraphGenerator.GenerateCollection(
            NewGraphGenerator.GenerateLayout(),
            NewGraphGenerator.GenerateLayout(NewGraphGenerator.OtherLayoutId));

        var graph = layoutCollection.ToGraph(layoutCollection.Layouts);

        graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.StartNodeId),
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.EndNodeId),
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.StartNodeId, NewGraphGenerator.OtherLayoutId),
            NewGraphGenerator.GenerateInternalId(NewGraphGenerator.EndNodeId, NewGraphGenerator.OtherLayoutId)
        ], true);
        graph.Edges.Count.ShouldBe(2);
        graph.Stations.Count.ShouldBe(2);
    }
}