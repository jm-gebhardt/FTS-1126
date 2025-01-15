using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
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
        var map = new TransportOrder.Domain.Models.Map.Map("map".ToGuid(), "map", [], 8, 8, 1, 1, 1, DateTime.UtcNow);
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []) { MapName = map.MapId };

        StationControllerTestFixture<GetStation, StationsRequestHandler> fixture = new StationControllerTestFixture<GetStation, StationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        var response = await fixture.GetControllerResultAsync(c => c.GetStation(station.Id));

        response.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetStation>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotStation>()).ShouldBeTrue();

        (await harness.Published.Any<GetStation>()).ShouldBeTrue();
        (await harness.Published.Any<GotStation>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task LockStation_ValidData_TryLockStation()
    {
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []) { MapName = "" };

        StationControllerTestFixture<TryLockStation, StationsRequestHandler> fixture = new StationControllerTestFixture<TryLockStation, StationsRequestHandler>()
            .WithStation(station)
            .WithUser(null, "user")
            .Build();

        bool result = await fixture.GetControllerResultValueAsync(c => c.LockStation(station.Id, "reason", "description", LockedOrderHandlingType.AllowFinish));

        result.ShouldBeTrue();

        (await fixture.MediatorTestHarness.Consumed.Any<TryLockStation>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<LockedUnlockedStation>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UnlockStation_ValidData_TryUnlockStation()
    {
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []) { MapName = "" };
        station.Lock("user", "reason", "description", LockedOrderHandlingType.AllowFinish);
        StationControllerTestFixture<TryUnlockStation, StationsRequestHandler> fixture = new StationControllerTestFixture<TryUnlockStation, StationsRequestHandler>()
            .WithStation(station)
            .WithActionGroups(new List<TransportOrder.Domain.Models.ActionGroup.ActionGroup>())
            .WithUser(null, "user")
            .Build();

        bool result = await fixture.GetControllerResultValueAsync(c => c.UnlockStation(station.Id));

        result.ShouldBeTrue();

        (await fixture.MediatorTestHarness.Consumed.Any<TryUnlockStation>()).ShouldBeTrue();
        (await fixture.MediatorTestHarness.Published.Any<LockedUnlockedStation>()).ShouldBeTrue();
    }
}