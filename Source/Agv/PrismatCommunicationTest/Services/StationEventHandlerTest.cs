using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
[DoNotParallelize]
public class StationEventHandlerTest
{
    [TestMethod]
    public async Task Consume_StationLocked_StationIsShortLoopTarget_NewShortLoopTargetSet()
    {
        string station = OrderGenerator.KaeserConfiguration.GetShortLoopTarget();
        var shortLoopTarget = new ShortLoopWeTargetStation(station, station);
        shortLoopTarget.LockedStations.Add(station);

        using var fixture = new StationEventHandlerTestFixture()
            .WithShortLoopTarget(shortLoopTarget);

        await fixture.TestHarness.Bus.Publish(new StationLocked(station));

        await fixture.ShouldHaveConsumedAsync<StationLocked>();

        fixture.VerifyShortLoopTargetUpdated(Times.Once());
        fixture.Configuration.GetShortLoopTarget().ShouldNotBe(station);
        shortLoopTarget.Current.ShouldNotBe(station);
        shortLoopTarget.Primary.ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_StationLocked_StationIsNotShortLoopTarget_ShortLoopTargetUntouched()
    {
        string station = OrderGenerator.KaeserConfiguration.GetShortLoopTarget();
        var shortLoopTarget = new ShortLoopWeTargetStation(station, station);

        using var fixture = new StationEventHandlerTestFixture()
            .WithShortLoopTarget(shortLoopTarget);

        await fixture.TestHarness.Bus.Publish(new StationLocked("another station"));

        await fixture.ShouldHaveConsumedAsync<StationLocked>();

        fixture.VerifyShortLoopTargetUpdated(Times.Never());
        fixture.Configuration.GetShortLoopTarget().ShouldBe(station);
        shortLoopTarget.Current.ShouldBe(station);
        shortLoopTarget.Primary.ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_StationIsPrimaryAndNotCurrentStation_StationReset()
    {
        string defaultStation = OrderGenerator.KaeserConfiguration.GetShortLoopTarget();
        const string nextStation = "next station";
        OrderGenerator.KaeserConfiguration.SetShortLoopTarget(nextStation);
        var shortLoopTarget = new ShortLoopWeTargetStation(defaultStation, nextStation);

        using var fixture = new StationEventHandlerTestFixture()
            .WithShortLoopTarget(shortLoopTarget);

        await fixture.PublishMessageAsync(new StationUnlocked(defaultStation.ToGraphNode()));

        fixture.VerifyShortLoopTargetUpdated(Times.Once());
        fixture.Configuration.GetShortLoopTarget().ShouldBe(shortLoopTarget.Primary);
        shortLoopTarget.Current.ShouldBe(defaultStation);
        shortLoopTarget.Primary.ShouldBe(defaultStation);
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_StationIsNotPrimaryStation_StationUntouched()
    {
        string defaultStation = OrderGenerator.KaeserConfiguration.GetShortLoopTarget();
        const string nextStation = "next station";
        OrderGenerator.KaeserConfiguration.SetShortLoopTarget(nextStation);
        var shortLoopTarget = new ShortLoopWeTargetStation(defaultStation, nextStation);

        using var fixture = new StationEventHandlerTestFixture()
            .WithShortLoopTarget(shortLoopTarget);

        await fixture.PublishMessageAsync(new StationUnlocked("another station".ToGraphNode()));

        fixture.VerifyShortLoopTargetUpdated(Times.Never());
        fixture.Configuration.GetShortLoopTarget().ShouldBe(shortLoopTarget.Current);
        shortLoopTarget.Current.ShouldBe(nextStation);
        shortLoopTarget.Primary.ShouldBe(defaultStation);
    }

    [TestMethod]
    public async Task Consume_AgvLeftStation_ValidData_PublishesTryQueueDelayedOrder()
    {
        using var fixture = new StationEventHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new AgvLeftStation("2430", "2410"));

        await fixture.ShouldHaveConsumedAsync<AgvLeftStation>();
        await fixture.ShouldHavePublishedAsync<TryQueueDelayedOrder>();
    }

    [TestMethod]
    public async Task Consumer_ReserveStation_ValidData_PublishesAddAgvToStation()
    {
        var station = new Station("source", "target", "node".ToGuid(), "node");
        var agv = Guid.NewGuid();
        using var fixture = new StationEventHandlerTestFixture()
            .WithStation(station)
            .WithOccupiedStation(null);

        await fixture.PublishMessageAsync(new ReserveStation(station.NodeName.ToGraphNode(), agv));

        await fixture.ShouldHavePublishedAsync<AddAgvToStation>();
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_ReserveStation_AgvOccupiesDifferentStation_PublishesRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "node".ToGuid(), "node");
        var occupiedStation = new Station("source", "target", "different node".ToGuid(), "different node");
        var agv = Guid.NewGuid();
        using var fixture = new StationEventHandlerTestFixture()
            .WithStation(station)
            .WithOccupiedStation(occupiedStation);

        await fixture.PublishMessageAsync(new ReserveStation(station.NodeName.ToGraphNode(), agv));

        await fixture.ShouldHavePublishedAsync<AddAgvToStation>();
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_ReserveStation_AgvAlreadyOccupiesStation_DoesNotPublishAddAgvToStation()
    {
        var station = new Station("source", "target", "source".ToGuid(), "source");
        var agv = Guid.NewGuid();
        using var fixture = new StationEventHandlerTestFixture()
            .WithStation(station)
            .WithOccupiedStation(station);

        await fixture.PublishMessageAsync(new ReserveStation(station.NodeName.ToGraphNode(), agv));

        await fixture.ShouldNotHavePublishedAsync<AddAgvToStation>();
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_FreeStationOccupancy_ValidData_PublishesRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "source".ToGuid(), "source");
        var agv = Guid.NewGuid();
        using var fixture = new StationEventHandlerTestFixture()
            .WithOccupiedStation(station);

        await fixture.PublishMessageAsync(new FreeStationOccupancy(station.Id, agv));

        await fixture.ShouldHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_FreeStationOccupancy_AgvOccupiesDifferentStation_DoesNotPublishRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "node".ToGuid(), "node");
        var agv = Guid.NewGuid();
        using var fixture = new StationEventHandlerTestFixture()
            .WithOccupiedStation(station);

        await fixture.PublishMessageAsync(new FreeStationOccupancy("different node".ToGuid(), agv));

        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_UnblockStation_ValidData_PublishesRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "source".ToGuid(), "node");
        var agv = Guid.NewGuid();
        station.AddAgv(agv);
        using var fixture = new StationEventHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UnblockStation(station.Id));
        await fixture.ShouldHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_UnblockStation_NoStation_DoesNotPublishRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "node".ToGuid(), "node");

        using var fixture = new StationEventHandlerTestFixture();

        await fixture.PublishMessageAsync(new UnblockStation(station.Id));
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromStation>();
    }

    [TestMethod]
    public async Task Consumer_UnblockStation_NoOccupancy_DoesNotPublishRemoveAgvFromStation()
    {
        var station = new Station("source", "target", "node".ToGuid(), "node");

        using var fixture = new StationEventHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UnblockStation(station.Id));
        await fixture.ShouldNotHavePublishedAsync<RemoveAgvFromStation>();
    }
}