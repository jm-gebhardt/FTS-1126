using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Map;

[TestClass]
public class MapControllerTest
{
    [TestMethod]
    public async Task GetBackgroundImage_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        var mapBackground = MapBackgroundGenerator.Generate();
        Guid correlationId = new("7FE9372B-4219-4D09-8885-966EE125A350");

        MapControllerTestFixture<GetBackgroundImage, MapRequestHandler> fixture = new MapControllerTestFixture<GetBackgroundImage, MapRequestHandler>()
            .AddMapRepositoryMock(mapBackground)
            .Build();

        var mapController = fixture.Controller;
        var response = await mapController.GetBackgroundImage(correlationId);

        var result = response as FileContentResult;
        byte[] resultValue = result!.FileContents;

        resultValue.ShouldNotBeEmpty();
        resultValue.ShouldBeEquivalentTo(mapBackground.Image);

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetBackgroundImage>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotBackgroundImage>()).ShouldBeTrue();

        (await harness.Published.Any<GetBackgroundImage>()).ShouldBeTrue();
        (await harness.Published.Any<GotBackgroundImage>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNodeStatesForMap_ValidData_RespondsNodeStates()
    {
        var nodeStates = new List<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName)>
        {
            (new NodeLock("nodeId".ToGraphNode(), NodeLockState.Free, false, null, DateTime.UtcNow), Guid.NewGuid(), null, null, null, "nodeId")
        };

        MapControllerTestFixture<GetMapNodeStates, NodeStateRequestHandler> fixture = new MapControllerTestFixture<GetMapNodeStates, NodeStateRequestHandler>(nodeStates)
            .Build();

        List<NodeStateDto> mappedNodeStates = nodeStates.Select(x =>
            {
                var dto = fixture.Mapper.Map<NodeStateDto>(x.NodeLock);
                dto = dto with
                {
                    StationId = x.StationId,
                    User = x.LockedByUser,
                    LockReason = x.LockReason,
                    LockDescription = x.LockDescription,
                    NodeId = x.NodeName
                };

                return dto;
            })
            .ToList();

        ICollection<NodeStateDto> actualNodeStates = fixture.GetControllerResultAsync(c => c.GetNodeStatesForMap(Guid.NewGuid())).Result;
        actualNodeStates.ShouldNotBeNull();
        actualNodeStates.Select(a => new { a.AgvCorrelationId, a.IsLocked, a.NodeId, a.NodeState })
            .ShouldBe(mappedNodeStates.Select(a => new { a.AgvCorrelationId, a.IsLocked, a.NodeId, a.NodeState }));

        (await fixture.MediatorTestHarness.Published.Any<GetMapNodeStates>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<GotMapNodeStates>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetMap_ShouldReturn_Metadata_WhenBackgroundExists()
    {
        var background = MapBackgroundGenerator.Generate();
        MapControllerTestFixture<GetMap, MapRequestHandler> fixture = new MapControllerTestFixture<GetMap, MapRequestHandler>()
            .AddMapRepositoryMock(background)
            .Build();
        var controller = fixture.Controller;
        ActionResult<MapDto> result = await controller.GetMap(background.CorrelationId);
        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as MapDto;
        dto.ShouldNotBeNull();
        dto.Width.ShouldBe(background.Width);
        dto.Height.ShouldBe(background.Height);
        (await fixture.MediatorTestHarness.Consumed.Any<GetMap>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<GotMap>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<GetMap>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<GotMap>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetMap_ShouldThrow_RequestException_WhenBackgroundMissing()
    {
        MapControllerTestFixture<GetMap, MapRequestHandler> fixture = new MapControllerTestFixture<GetMap, MapRequestHandler>()
            .AddMapRepositoryMock(null)
            .Build();
        var controller = fixture.Controller;
        await Should.ThrowAsync<RequestException>(controller.GetMap(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task SetBackground_ShouldReturn_Ok_WhenValidImage()
    {
        MapControllerTestFixture<SetBackground, MapCommandHandler> fixture = new MapControllerTestFixture<SetBackground, MapCommandHandler>()
            .AddMapRepositoryMock(MapBackgroundGenerator.Generate())
            .Build();
        var controller = fixture.Controller;
        byte[] image = MapBackgroundGenerator.Image1;
        var formFile = new FormFile(new MemoryStream(image), 0, image.Length, "file", "bg.png");
        var dto = new SetBackgroundDto
        {
            Resolution = 0.1,
            XOffset = 1,
            YOffset = 2,
            File = formFile
        };
        var response = await controller.SetBackground(Guid.NewGuid(), dto);
        response.ShouldBeOfType<OkResult>();
        (await fixture.MediatorTestHarness.Consumed.Any<SetBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<Success>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<SetBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<Success>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldReturn_NoContent_WhenSuccessful()
    {
        var correlationId = Guid.NewGuid();
        MapControllerTestFixture<DeleteBackground, MapCommandHandler> fixture = new MapControllerTestFixture<DeleteBackground, MapCommandHandler>()
            .AddMapRepositoryMock(MapBackgroundGenerator.Generate())
            .Build();
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(correlationId)).ReturnsAsync(true);
        fixture.MapRepositoryMock.Setup(r => r.GetBackgroundImageAsync(correlationId)).ReturnsAsync(MapBackgroundGenerator.Image1);
        var controller = fixture.Controller;
        var response = await controller.DeleteBackground(correlationId);
        response.ShouldBeOfType<NoContentResult>();
        (await fixture.MediatorTestHarness.Consumed.Any<DeleteBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<Success>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<DeleteBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<Success>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldReturn_NoContent_WhenNoCustomBackgroundWasExisting()
    {
        var correlationId = Guid.NewGuid();
        MapControllerTestFixture<DeleteBackground, MapCommandHandler> fixture = new MapControllerTestFixture<DeleteBackground, MapCommandHandler>()
            .AddMapRepositoryMock(null)
            .Build();
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(correlationId)).ReturnsAsync(false);
        var controller = fixture.Controller;
        var response = await controller.DeleteBackground(correlationId);
        response.ShouldBeOfType<NoContentResult>();
        (await fixture.MediatorTestHarness.Consumed.Any<DeleteBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Consumed.Any<Success>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<DeleteBackground>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<Success>()).ShouldBeTrue();
    }
}