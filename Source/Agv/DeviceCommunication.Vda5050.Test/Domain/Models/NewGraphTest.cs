using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Graph;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Models;

[TestClass]
public class NewGraphTest
{
    [TestMethod]
    public void ConvertToGraph_ValidData_CorrectlyConverted()
    {
        var layoutCollection = DomainLifGenerator.SampleCollection;

        var graph = LifToGraphConverter.ToGraphSegment(layoutCollection, DomainLifGenerator.AgvTypeIdentifiers, NullLogger.Instance);

        graph.Edges.Count.ShouldBe(1);
        graph.Nodes.Count.ShouldBe(2);
        graph.Stations.Count.ShouldBe(1);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_ConvertsAllLayouts()
    {
        var layout = DomainLifGenerator.GenerateLayout();
        var layoutCollection = DomainLifGenerator.GenerateCollection(layout, DomainLifGenerator.GenerateLayout(DomainLifGenerator.OtherLayoutId));

        var graph = LifToGraphConverter.ToGraphSegment(layoutCollection, DomainLifGenerator.AgvTypeIdentifiers, NullLogger.Instance);

        graph.Nodes.Select(n => n.Id).ShouldBe(
        [
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.StartNodeId),
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.EndNodeId),
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.StartNodeId, DomainLifGenerator.OtherLayoutId),
            DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.EndNodeId, DomainLifGenerator.OtherLayoutId)
        ], ignoreOrder: true);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_EdgeAndStationReferenceTheNodesOfTheLayout()
    {
        var layoutCollection = DomainLifGenerator.GenerateCollection(DomainLifGenerator.GenerateLayout());

        var graph = LifToGraphConverter.ToGraphSegment(layoutCollection, DomainLifGenerator.AgvTypeIdentifiers, NullLogger.Instance);

        var startNodeId = DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.StartNodeId);
        var endNodeId = DomainLifGenerator.GenerateInternalNodeId(DomainLifGenerator.EndNodeId);
        var edge = graph.Edges.ShouldHaveSingleItem();
        edge.Id.ShouldBe(DomainLifGenerator.GenerateInternalEdgeId(DomainLifGenerator.EdgeId));
        edge.StartNodeId.ShouldBe(startNodeId);
        edge.EndNodeId.ShouldBe(endNodeId);
        var station = graph.Stations.ShouldHaveSingleItem();
        station.Id.ShouldBe(DomainLifGenerator.GenerateInternalStationId(DomainLifGenerator.StationId));
        station.InteractionNodeIds.ShouldHaveSingleItem().ShouldBe(startNodeId);
    }

    [TestMethod]
    public void ConvertToGraph_ValidData_TakesNodeLevelIdFromLayout()
    {
        var layoutCollection = DomainLifGenerator.GenerateCollection(DomainLifGenerator.GenerateLayout());

        var graph = LifToGraphConverter.ToGraphSegment(layoutCollection, DomainLifGenerator.AgvTypeIdentifiers, NullLogger.Instance);

        graph.Nodes.ShouldAllBe(n => n.LevelId == DomainLifGenerator.LevelId);
    }
}