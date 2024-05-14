using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

[TestClass]
public class StationControllerTests
{
    [TestMethod]
    public async Task GetStation_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        string correlationId = Guid.NewGuid().ToString();
        var graphNode = NodeGenerator.GenerateNode(nodeId: correlationId, nodeType: AgvGraph.NodeType.PickDrop);

        StationControllerTestFixture<GetStation, GetStationsRequestHandler> fixture = new StationControllerTestFixture<GetStation, GetStationsRequestHandler>()
            .WithGraphNode(graphNode)
            .Build();

        var response = await fixture.GetControllerResultAsync(c => c.GetStation(correlationId));

        response.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetStation>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotStation>()).ShouldBeTrue();

        (await harness.Published.Any<GetStation>()).ShouldBeTrue();
        (await harness.Published.Any<GotStation>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetStation_ThrowsException_WhereNodeTypeIsNotStation()
    {
        string correlationId = Guid.NewGuid().ToString();
        var graphNode = NodeGenerator.GenerateNode(nodeId: correlationId, nodeType: AgvGraph.NodeType.WayPoint);

        StationControllerTestFixture<GetStation, GetStationsRequestHandler> fixture = new StationControllerTestFixture<GetStation, GetStationsRequestHandler>()
            .WithGraphNode(graphNode)
            .Build();

        var requestException = await MediatorShould.ThrowRequestExceptionAsync<InvalidNodeTypeException>(async () => await fixture.GetControllerResultAsync(c => c.GetStation(correlationId)));

        requestException.Message.ShouldBe($"Node {graphNode.Id} has invalid type {graphNode.Type}, expected station");
    }
}