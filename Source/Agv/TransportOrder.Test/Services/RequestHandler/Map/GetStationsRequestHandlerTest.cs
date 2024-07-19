using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Graph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class GetStationsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetStations_ValidData_ReturnsCorrectStations()
    {
        const double padding = 0.2;
        const double maxWidth = 3.0;
        const double maxLength = 1.7;
        const string stationPickName = "StationPick";
        const string notAStation = "NotAStation";
        var mapCorrelationId = Guid.NewGuid();
        var agvCorrelationId1 = Guid.NewGuid();
        var agvCorrelationId2 = Guid.NewGuid();
        var agvData1 = AgvDataGenerator.GenerateAgvData(agvCorrelationId1, width: 1.5, length: maxLength);
        var agvData2 = AgvDataGenerator.GenerateAgvData(agvCorrelationId2, width: maxWidth, length: 1.2);
        var agvCorrelationIds = new List<Guid> { agvCorrelationId1, agvCorrelationId2 };
        var agvsData = new List<AgvData> { agvData1, agvData2 };
        var graphNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(nodeId: "StationPickDrop", nodeType: AgvGraph.NodeType.PickDrop),
            NodeGenerator.GenerateNode(nodeId: "StationCharge", nodeType: AgvGraph.NodeType.Charge),
            NodeGenerator.GenerateNode(nodeId: "StationDrop", nodeType: AgvGraph.NodeType.Drop),
            NodeGenerator.GenerateNode(nodeId: "StationPark", nodeType: AgvGraph.NodeType.Park),
            NodeGenerator.GenerateNode(nodeId: stationPickName, nodeType: AgvGraph.NodeType.Pick),
            NodeGenerator.GenerateNode(nodeId: notAStation, nodeType: AgvGraph.NodeType.WayPoint)
        };
        List<AgvGraph> agvGraphs = agvCorrelationIds.Select(_ => graphNodes)
            .Select(nodes => AgvGraphGenerator.CreateDefaultGraph(mapCorrelationId, nodes))
            .ToList();
        GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler>()
            .WithGraph(new Graph { Nodes = graphNodes })
            .WithAgvGraphs(agvCorrelationIds, agvGraphs)
            .WithAgvsRelatedToMap(mapCorrelationId, agvCorrelationIds, agvsData)
            .Build();

        Response<GotMapStations> response = await fixture.GetResponseAsync<GotMapStations>(new GetMapStations(mapCorrelationId));

        List<TransportOrder.Messages.MessageContent.Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(5);
        gotStations.Count(s => s.Id == stationPickName).ShouldBe(1);
        gotStations.Count(s => s.Id.StartsWith(notAStation)).ShouldBe(0);
        gotStations.ForEach(s =>
        {
            s.Width.ShouldBeEquivalentTo(maxWidth + padding);
            s.Length.ShouldBeEquivalentTo(maxLength + padding);
            s.StationType.ShouldNotBe(AgvGraph.NodeType.WayPoint);
        });
    }

    [TestMethod]
    public async Task Consume_GetStations_NoAgvsAvailable_ReturnsCorrectStations()
    {
        const double stationPadding = 0.2;
        const string stationPickName = "StationPick";
        const string notAStation = "NotAStation";
        var mapCorrelationId = Guid.NewGuid();
        var graphNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(nodeId: "StationPickDrop", nodeType: AgvGraph.NodeType.PickDrop),
            NodeGenerator.GenerateNode(nodeId: "StationCharge", nodeType: AgvGraph.NodeType.Charge),
            NodeGenerator.GenerateNode(nodeId: "StationDrop", nodeType: AgvGraph.NodeType.Drop),
            NodeGenerator.GenerateNode(nodeId: "StationPark", nodeType: AgvGraph.NodeType.Park),
            NodeGenerator.GenerateNode(nodeId: stationPickName, nodeType: AgvGraph.NodeType.Pick),
            NodeGenerator.GenerateNode(nodeId: notAStation, nodeType: AgvGraph.NodeType.WayPoint)
        };
        GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler>()
            .WithGraph(new Graph { Nodes = graphNodes })
            .Build();

        Response<GotMapStations> response = await fixture.GetResponseAsync<GotMapStations>(new GetMapStations(mapCorrelationId));

        List<TransportOrder.Messages.MessageContent.Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(5);
        gotStations.Count(s => s.Id == stationPickName).ShouldBe(1);
        gotStations.Count(s => s.Id.StartsWith(notAStation)).ShouldBe(0);
        gotStations.ForEach(s =>
        {
            s.Width.ShouldBeEquivalentTo(stationPadding);
            s.Length.ShouldBeEquivalentTo(stationPadding);
            s.StationType.ShouldNotBe(AgvGraph.NodeType.WayPoint);
        });
    }

    [TestMethod]
    public async Task Consume_GetStations_NoStationsAvailable_ReturnsEmptyList()
    {
        const string notAStation = "NotAStation";
        var mapCorrelationId = Guid.NewGuid();
        var graphNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(nodeId: notAStation, nodeType: AgvGraph.NodeType.WayPoint)
        };
        GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler>()
            .WithGraph(new Graph { Nodes = graphNodes })
            .Build();

        Response<GotMapStations> response = await fixture.GetResponseAsync<GotMapStations>(new GetMapStations(mapCorrelationId));

        List<TransportOrder.Messages.MessageContent.Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(0);
    }
    
     [TestMethod]
    public async Task Consume_GetStation_ValidData_ReturnsCorrectStation()
    {
        const double maxWidth = 3.0;
        const double maxLength = 1.7;
        var mapCorrelationId = Guid.NewGuid();
        var agvCorrelationId1 = Guid.NewGuid();
        var agvCorrelationId2 = Guid.NewGuid();
        var agvData1 = AgvDataGenerator.GenerateAgvData(agvCorrelationId1, width: 1.5, length: maxLength);
        var agvData2 = AgvDataGenerator.GenerateAgvData(agvCorrelationId2, width: maxWidth, length: 1.2);
        var agvCorrelationIds = new List<Guid> { agvCorrelationId1, agvCorrelationId2 };
        var agvsData = new List<AgvData> { agvData1, agvData2 };
        var graphNodes = new List<GraphNode>
        {
            NodeGenerator.GenerateNode(nodeId: mapCorrelationId.ToString(), nodeType: AgvGraph.NodeType.PickDrop),
            NodeGenerator.GenerateNode(nodeId: "StationCharge", nodeType: AgvGraph.NodeType.Charge)
        };

        GetStationsRequestHandlerTestFixture<GetStation, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetStation, GetStationsRequestHandler>()
            .WithGraphNode(new Graph { Nodes = graphNodes }, mapCorrelationId.ToString())
            .WithAgvsRelatedToMap(mapCorrelationId, agvCorrelationIds, agvsData)
            .Build();

        Response<GotStation> response = await fixture.GetResponseAsync<GotStation>(new GetStation(mapCorrelationId.ToString()));

        var gotStation = response.Message.Station;
        gotStation.ShouldNotBeNull();
        gotStation.Id.ShouldBe(mapCorrelationId.ToString());
        gotStation.ShouldBeOfType<TransportOrder.Messages.MessageContent.Station>();
    }
}