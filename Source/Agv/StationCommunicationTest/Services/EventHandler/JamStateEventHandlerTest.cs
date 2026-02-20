using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.Services.EventHandler;

[TestClass]
public class JamStateEventHandlerTest
{
    [TestMethod]
    public async Task Consume_JamStateReceived_ValidData_PublishesUpdateJamState()
    {
        var fixture = new JamStateEventHandlerTestFixture();

        var jamStateReceived = new JamStateReceived("PS01", 1, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(jamStateReceived);

        await fixture.ShouldHaveConsumedAsync<JamStateReceived>();

        await fixture.ShouldHavePublishedAsync<UpdateJamState>();

        var updateJamState = await fixture.GetFirstPublishedMessageAsync<UpdateJamState>();
        updateJamState.StationId.ShouldBe(jamStateReceived.Station.ToGuid());
        updateJamState.Status.ShouldBe(jamStateReceived.Status);
        updateJamState.TimeStamp.ShouldBe(jamStateReceived.TimeStamp);
    }
    
    [TestMethod]
    public async Task Consume_StationUnlocked_StationHasJamState_PublishesRequestJamState()
    {
        const string stationName = "PS01";

        var fixture = new JamStateEventHandlerTestFixture()
            .WithStation(nodeName: stationName, hasJamState: true);

        var stationUnlocked = new StationUnlocked(new GraphNode(stationName.ToGuid(), stationName));

        await fixture.TestHarness.Bus.Publish(stationUnlocked);

        await fixture.ShouldHaveConsumedAsync<StationUnlocked>();

        await fixture.ShouldHavePublishedAsync<RequestJamState>();

        var requestJamState = await fixture.GetFirstPublishedMessageAsync<RequestJamState>();
        requestJamState.Station.ShouldBe(stationName);
    }
     
    [TestMethod]
    public async Task Consume_StationUnlocked_StationNoJamState_DoesNotPublishesRequestJamState()
    {
        const string stationName = "PS01";

        var fixture = new JamStateEventHandlerTestFixture()
            .WithStation(nodeName: stationName, hasJamState: false);

        var stationUnlocked = new StationUnlocked(new GraphNode(stationName.ToGuid(), stationName));

        await fixture.TestHarness.Bus.Publish(stationUnlocked);

        await fixture.ShouldHaveConsumedAsync<StationUnlocked>();

        await fixture.ShouldNotHavePublishedAsync<RequestJamState>();
    }
   
    [TestMethod]
    public async Task Consume_JamStateRequested_ValidData_PublishesUpdateJamState()
    {
        const string stationName = "PS01";

        var fixture = new JamStateEventHandlerTestFixture()
            .WithStation(nodeName: stationName, hasJamState: true);

        var jamStateRequested = new JamStateRequested(new GraphNode(stationName.ToGuid(), stationName));

        await fixture.TestHarness.Bus.Publish(jamStateRequested);

        await fixture.ShouldHaveConsumedAsync<JamStateRequested>();

        await fixture.ShouldHavePublishedAsync<RequestJamState>();

        var requestJamState = await fixture.GetFirstPublishedMessageAsync<RequestJamState>();
        requestJamState.Station.ShouldBe(stationName);
    }
}