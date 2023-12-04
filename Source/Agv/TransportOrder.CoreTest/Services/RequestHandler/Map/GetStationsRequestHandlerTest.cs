using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using GraphNode = Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.GraphNode;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Map;

[TestClass]
public class GetStationsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetStations_ValidData_ReturnsCorrectStations()
    {
        const double padding = 0.2;
        const double maxWidth = 3.0;
        const double maxLength = 1.7;
        const string stationPickCommonToAgvs = "StationIdCommonToAgvs";
        const string notAStation = "NotAStation";
        var mapCorrelationId = Guid.NewGuid();
        var agvCorrelationId1 = Guid.NewGuid();
        var agvCorrelationId2 = Guid.NewGuid();
        var agvData1 = AgvDataGenerator.GenerateAgvData(agvCorrelationId1, width: 1.5, length: maxLength);
        var agvData2 = AgvDataGenerator.GenerateAgvData(agvCorrelationId2, width: maxWidth, length: 1.2);
        var agvCorrelationIds = new List<Guid> {agvCorrelationId1, agvCorrelationId2};
        var agvsData = new List<AgvData> {agvData1, agvData2};
        List<AgvGraph> agvGraphs = agvCorrelationIds.Select(agvCorrelationId => new List<GraphNode>
            {
                NodeGenerator.GenerateNode(nodeId: $"StationPickDrop-{agvCorrelationId}", nodeType: AgvGraph.NodeType.PickDrop),
                NodeGenerator.GenerateNode(nodeId: $"StationCharge-{agvCorrelationId}", nodeType: AgvGraph.NodeType.Charge),
                NodeGenerator.GenerateNode(nodeId: $"StationDrop-{agvCorrelationId}", nodeType: AgvGraph.NodeType.Drop),
                NodeGenerator.GenerateNode(nodeId: $"StationPark-{agvCorrelationId}", nodeType: AgvGraph.NodeType.Park),
                NodeGenerator.GenerateNode(nodeId: stationPickCommonToAgvs, nodeType: AgvGraph.NodeType.Pick),
                NodeGenerator.GenerateNode(nodeId: $"{notAStation}-{agvCorrelationId}", nodeType: AgvGraph.NodeType.WayPoint)
            })
            .Select(nodes => AgvGraphGenerator.CreateDefaultGraph(mapCorrelationId, nodes))
            .ToList();
        var fixture = new GetStationsRequestHandlerTestFixture()
            .WithAgvsRelatedToMap(mapCorrelationId, agvCorrelationIds, agvsData)
            .WithAgvGraphs(agvCorrelationIds, agvGraphs)
            .Build();

        Response<GotStations> response = await fixture.GetResponseAsync<GotStations>(new GetStations(mapCorrelationId));

        List<Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(9);
        gotStations.Count(s => s.Id == stationPickCommonToAgvs).ShouldBe(1);
        gotStations.Count(s => s.Id.StartsWith(notAStation)).ShouldBe(0);
        gotStations.ForEach(s =>
        {
            s.Width.ShouldBeEquivalentTo(maxWidth + padding);
            s.Length.ShouldBeEquivalentTo(maxLength + padding);
        });
    }
}