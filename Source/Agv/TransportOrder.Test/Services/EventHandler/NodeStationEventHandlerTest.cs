using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class NodeStationEventHandlerTest
{
    [TestMethod]
    public async Task Consume_StationUnlocked_NoChargingAgvs_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new NodeStationEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new StationUnlocked("station".ToGraphNode()));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldNotHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_ChargingAgvs_PublishesTryReactivateChargingAgvs()
    {
        var fixture = new NodeStationEventHandlerTestFixture()
            .WithChargingAgvs([AgvGenerator.GenerateChargingAgv()]);

        await fixture.PublishMessageAsync(new StationUnlocked("station".ToGraphNode()));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    public async Task Consume_JamStateUpdated_ValidData_PublishesUpdateStationJamState()
    {
        var fixture = new NodeStationEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new JamStateUpdated("station".ToGuid(), true));

        await fixture.ShouldHavePublishedAsync<UpdateStationJamState>();
    }

    [TestMethod]
    public async Task Consume_StationConfigured_ValidData_PublishesUpdateStationAdditionalInfo()
    {
        var fixture = new NodeStationEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new StationConfigured("station".ToGuid(), new Dictionary<string, object?>()));

        await fixture.ShouldHavePublishedAsync<UpdateStationAdditionalInfo>();
    }

    [TestMethod]
    public async Task Consume_StationIsOnlineChanged_False_PublishesUpdateStationIsOnline()
    {
        var fixture = new NodeStationEventHandlerTestFixture();
        var stationId = "PS01".ToGuid();

        await fixture.PublishMessageAsync(new StationIsOnlineChanged(stationId, false));

        await fixture.ShouldHavePublishedAsync<UpdateStationIsOnline>();
    }

    [TestMethod]
    public async Task Consume_StationIsOnlineChanged_True_PublishesUpdateStationIsOnline()
    {
        var fixture = new NodeStationEventHandlerTestFixture();
        var stationId = "PS01".ToGuid();

        await fixture.PublishMessageAsync(new StationIsOnlineChanged(stationId, true));

        await fixture.ShouldHavePublishedAsync<UpdateStationIsOnline>();
    }
}