using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

[TestClass]
public class StationEventHandlerTest
{
    [TestMethod]
    public async Task Consume_StationUnlocked_NoChargingAgvs_PublishesTryReactivateParkingAgvs()
    {
        var fixture = new StationEventHandlerTestFixture();

        await fixture.PublishMessageAsync<StationUnlocked, string>(new StationUnlocked("station"));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldNotHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_ChargingAgvs_PublishesTryReactivateChargingAgvs()
    {
        var fixture = new StationEventHandlerTestFixture()
            .WithChargingAgvs([AgvGenerator.GenerateChargingAgv()]);

        await fixture.PublishMessageAsync<StationUnlocked, string>(new StationUnlocked("station"));

        await fixture.ShouldHavePublishedAsync<TryReactivateParkingAgvs>();
        await fixture.ShouldHavePublishedAsync<TryReactivateChargingAgv>();
    }

    [TestMethod]
    public async Task Consume_JamStateUpdated_ValidData_PublishesUpdateStationJamState()
    {
        var fixture = new StationEventHandlerTestFixture();

        await fixture.PublishMessageAsync<JamStateUpdated, string>(new JamStateUpdated("station", true));

        await fixture.ShouldHavePublishedAsync<UpdateStationJamState>();
    }
}