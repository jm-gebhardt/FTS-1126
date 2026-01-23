using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class StationsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetMapStations_ValidData_ReturnsStations()
    {
        var map = MapGenerator.Generate();
        var station = new Station("name", AgvGraph.NodeType.Park, []) { MapId = map.CorrelationId };
        StationsRequestHandlerTestFixture<GetMapStations, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<GetMapStations, StationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        Response<GotMapStations> response = await fixture.GetResponseAsync<GotMapStations>(new GetMapStations(map.CorrelationId));

        ICollection<Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(1);
        gotStations.First().ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_GetStations_ValidData_ReturnsStations()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        StationsRequestHandlerTestFixture<GetStations, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<GetStations, StationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<GotStations> response = await fixture.GetResponseAsync<GotStations>(new GetStations(Guid.NewGuid()));

        ICollection<Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(1);
        gotStations.First().ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_GetStation_ValidData_ReturnsStations()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        StationsRequestHandlerTestFixture<GetStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<GetStation, StationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<GotStation> response = await fixture.GetResponseAsync<GotStation>(new GetStation(station.Id));

        response.Message.Station.ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_TryUnlockStation_ValidData_PublishesUnlockStation_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: station.Name, pickActionState: ActionState.Paused);
        station.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
        StationsRequestHandlerTestFixture<TryUnlockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryUnlockStation, StationsRequestHandler>()
            .WithActionGroups([pickDropActionGroup])
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryUnlockStation(station.Id, "User"));
        response.Message.Result.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<UnlockStation>();
    }

    [TestMethod]
    public async Task Consume_TryUnlockStation_StationNotLocked_DoesNotPublishUnlockStation_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        StationsRequestHandlerTestFixture<TryUnlockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryUnlockStation, StationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryUnlockStation(station.Id, "User"));
        response.Message.Result.ShouldBeFalse();
        await fixture.ShouldNotHavePublishedAsync<UnlockStation>();
    }

    [TestMethod]
    public async Task Consume_TryLockStation_NotStartedChargeParkActionGroups_PublishLockStationCancelActionGroup_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(targetNodeName: station.Name);
        var parkActionGroup = AgvActionGenerator.GenerateMoveActionGroup(targetNodeId: station.Name);
        StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithActionGroups([parkActionGroup, chargeActionGroup])
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryLockStation(station.Id, "user", "reason", "description", LockedOrderHandlingType.AllowFinish));
        response.Message.Result.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<LockStation>();
        (await fixture.TestHarness.Published.Any<CancelActionGroup>(m => m.Context.Message == new CancelActionGroup(chargeActionGroup.CorrelationId, CancelReason.LockedStation))).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<CancelActionGroup>(m => m.Context.Message == new CancelActionGroup(parkActionGroup.CorrelationId, CancelReason.LockedStation))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryLockStation_StartedOrders_Abort_PublishLockStationAbortActionGroup_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: station.Name, pickActionState: ActionState.Started);
        StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithActionGroups([pickDropActionGroup])
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryLockStation(station.Id, "user", "reason", "description", LockedOrderHandlingType.Abort));
        response.Message.Result.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<LockStation>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
        (await fixture.TestHarness.Published.Any<AbortActionGroup>(m => m.Context.Message == new AbortActionGroup(pickDropActionGroup.CorrelationId, AbortReason.LockedStation))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryLockStation_StartedOrders_Pause_PublishLockStationPauseActionGroup_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: station.Name, pickActionState: ActionState.Started);
        StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithActionGroups([pickDropActionGroup])
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryLockStation(station.Id, "user", "reason", "description", LockedOrderHandlingType.OnHold));
        response.Message.Result.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<LockStation>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
        (await fixture.TestHarness.Published.Any<PauseActionGroup>(m => m.Context.Message == new PauseActionGroup(pickDropActionGroup.CorrelationId, PauseReason.LockedStation))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TryLockStation_StartedOrders_AllowFinish_PublishLockStation_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(destination: station.Name, pickActionState: ActionState.Started);
        StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithActionGroups([pickDropActionGroup])
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryLockStation(station.Id, "user", "reason", "description", LockedOrderHandlingType.AllowFinish));
        response.Message.Result.ShouldBeTrue();
        await fixture.ShouldHavePublishedAsync<LockStation>();
        await fixture.ShouldNotHavePublishedAsync<CancelActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<AbortActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<PauseActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryLockStation_StationIsLocked_DoesNotPublishLockStation_RespondsLockedUnlockedStation()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        station.Lock("user", "reason", "description", LockedOrderHandlingType.Abort);
        StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationsRequestHandlerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<LockedUnlockedStation> response = await fixture.GetResponseAsync<LockedUnlockedStation>(new TryLockStation(station.Id, "user", "reason", "description", LockedOrderHandlingType.AllowFinish));
        response.Message.Result.ShouldBeFalse();
        await fixture.ShouldNotHavePublishedAsync<LockStation>();
    }
}