using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Map;

[TestClass]
public class MapControllerTest
{
    [TestMethod]
    public async Task GetMap_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        byte[] imageByteArray = Convert.FromBase64String(MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>.Base64String);
        Guid correlationId = new("7FE9372B-4219-4D09-8885-966EE125A350");

        MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler> fixture = new MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>()
            .AddMapRepositoryMock()
            .Build();

        var mapController = fixture.Controller;
        var response = await mapController.GetBackgroundImage(correlationId);

        var result = response as FileContentResult;
        byte[] resultValue = result!.FileContents;

        resultValue.ShouldNotBeEmpty();
        resultValue.ShouldBeEquivalentTo(imageByteArray);

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapImage>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapImage>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNodeStatesForMap_ValidData_RespondsNodeStates()
    {
        var nodeStates = new List<NodeLock> { new("NodeId", NodeLockState.Free, false, null, DateTime.UtcNow) };
        MapControllerTestFixture<GetMapNodeStates, NodeStateRequestHandler> fixture = new MapControllerTestFixture<GetMapNodeStates, NodeStateRequestHandler>(nodeStates)
            .Build();

        ICollection<NodeStateDto> actualNodeStates = fixture.GetControllerResultAsync(c => c.GetNodeStatesForMap(Guid.NewGuid())).Result;
        actualNodeStates.ShouldNotBeNull();
        actualNodeStates.Select(a => new { a.AgvCorrelationId, a.IsLocked, a.NodeId, a.NodeState })
            .ShouldBe(fixture.Mapper.Map<ICollection<NodeStateDto>>(nodeStates).Select(a => new { a.AgvCorrelationId, a.IsLocked, a.NodeId, a.NodeState }));
        
        (await fixture.MediatorTestHarness.Published.Any<GetMapNodeStates>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<GotMapNodeStates>()).ShouldBeTrue();
    }
}