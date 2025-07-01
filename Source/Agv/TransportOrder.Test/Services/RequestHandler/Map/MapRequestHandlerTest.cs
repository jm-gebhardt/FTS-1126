using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        Response<GotMap> response = await fixture.GetResponseAsync<GotMap>(new GetMap(fixture.Maps[0].CorrelationId));

        fixture.MapRepositoryMock.Verify(m => m.GetMapAsync(fixture.Maps[0].CorrelationId), Times.Once);
        response.Message.Map.CorrelationId.ShouldBe(fixture.Maps[0].CorrelationId);
    }

    [TestMethod]
    public async Task GetMaps_ShouldReturnListOfMaps_WhereMapRepositoryIsContained()
    {
        var fixture = new MapRequestTestFixture<GetMapList, MapRequestHandler>();

        Response<GotMapList> response = await fixture.GetResponseAsync<GotMapList>(new GetMapList());
        List<TransportOrder.Messages.Requests.Map.Map> maps = response.Message.Maps;

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

        await fixture.GetResponseAsync<Success>(new TrySettingNodeApproachingProperties(fixture.Maps[0].CorrelationId, "nodeId", 12, 12, DockingDirection.Both));

        await fixture.ShouldHavePublishedAsync<SetNodeApproachingProperties>();
    }
}