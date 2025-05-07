using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class StationEventHandlerTest
{
    [TestMethod]
    public async Task Consume_StationLocked_StationIsShortLoopTarget_NewShortLoopTargetSet()
    {
        string station = OrderGenerator.KaeserConfiguration.GetShortLoopTarget();
        var shortLoopTarget = new ShortLoopWeTargetStation(station, station);
        
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
        var shortLoopTarget = new ShortLoopWeTargetStation(defaultStation, nextStation );

        using var fixture = new StationEventHandlerTestFixture()
            .WithShortLoopTarget(shortLoopTarget);

        await fixture.TestHarness.Bus.Publish(new StationUnlocked(defaultStation));

        await fixture.ShouldHaveConsumedAsync<StationUnlocked>();

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

        await fixture.TestHarness.Bus.Publish(new StationUnlocked("another station"));

        await fixture.ShouldHaveConsumedAsync<StationUnlocked>();

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
}