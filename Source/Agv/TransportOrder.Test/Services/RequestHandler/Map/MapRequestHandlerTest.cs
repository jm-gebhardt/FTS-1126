using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class MapRequestHandlerTest
{
    [TestMethod]
    public async Task GetMap_ShouldReturnMap_WhenMapExists()
    {
        var fixture = new MapRequestTestFixture<GetMap, MapRequestHandler>();

        var response = await fixture.GetResponseAsync<GotMap>(new GetMap(fixture.Maps[0].CorrelationId));

        fixture.MapRepositoryMock.Verify(m => m.GetMapAsync(fixture.Maps[0].CorrelationId), Times.Once);
        response.Message.Map.CorrelationId.ShouldBe(fixture.Maps[0].CorrelationId);
    }

    [TestMethod]
    public async Task GetMaps_ShouldReturnListOfMaps_WhereMapRepositoryIsContained()
    {
        var fixture = new MapRequestTestFixture<GetMapList, MapRequestHandler>();

        var response = await fixture.GetResponseAsync<GotMapList>(new GetMapList());
        var maps = response.Message.Maps;

        maps.ShouldNotBeEmpty();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TrySettingNodeApproachingProperties_ValidData_PublishesSetNodeApproachingProperties()
    {
        var fixture = new MapRequestTestFixture<TrySettingNodeApproachingProperties, MapRequestHandler>();

        await fixture.GetResponseAsync<Success>(new TrySettingNodeApproachingProperties(fixture.Maps[0].CorrelationId, "nodeId".ToGuid(), 12, 12, DockingDirection.Both));

        await fixture.ShouldHavePublishedAsync<SetNodeApproachingProperties>();
    }

    [TestMethod]
    public async Task GetBackgroundImage_ShouldReturn_ImageByteArray_WhenBackgroundExists()
    {
        var mapBackground = MapBackgroundGenerator.Generate();
        var fixture = new MapRequestTestFixture<GetBackgroundImage, MapRequestHandler>()
            .WithMapBackground(mapBackground);

        var response = await fixture.GetResponseAsync<GotBackgroundImage>(new GetBackgroundImage(mapBackground.CorrelationId));

        response.Message.Image.ShouldNotBeEmpty();
        response.Message.Image.Length.ShouldBe(mapBackground.Image.Length);

        var harness = fixture.MediatorTestHarness;
        (await harness.Consumed.Any<GetBackgroundImage>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotBackgroundImage>()).ShouldBeTrue();
        (await harness.Published.Any<GetBackgroundImage>()).ShouldBeTrue();
        (await harness.Published.Any<GotBackgroundImage>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetBackgroundImage_ShouldThrow_MapNotFoundException_WhenImageMissing()
    {
        var mapBackground = MapBackgroundGenerator.Generate(image: []);
        var fixture = new MapRequestTestFixture<GetBackgroundImage, MapRequestHandler>()
            .WithMapBackground(mapBackground);

        await MediatorShould.ThrowRequestExceptionAsync<MapNotFoundException>(async () => await fixture.GetResponseAsync<GotBackgroundImage>(new GetBackgroundImage(mapBackground.CorrelationId)));
    }
}