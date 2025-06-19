using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

[TestClass]
public class JamStateCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateJamState_NewJamState_AddsJamState()
    {
        var jamState = new JamState("PS01", 1, DateTime.UtcNow);

        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(null);

        var updateJamState = new UpdateJamState(jamState.Station, jamState.State, jamState.LastStateChangeTime);

        await fixture.TestHarness.Bus.Publish(updateJamState);

        await fixture.ShouldHaveConsumedAsync<UpdateJamState>();

        fixture.VerifyJamStateAdded(Times.Once());
        fixture.VerifyJamStateUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateJamState_ExistingJamState_UpdatesJamState()
    {
        var jamState = new JamState("PS01", 1, DateTime.UtcNow);

        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(jamState);

        var updateJamState = new UpdateJamState(jamState.Station,0, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(updateJamState);

        await fixture.ShouldHaveConsumedAsync<UpdateJamState>();

        fixture.VerifyJamStateAdded(Times.Never());
        fixture.VerifyJamStateUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_GetJamState_NoJamState_RespondsGotJamStateFalse()
    {
        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(null);

        var getJamState = new GetJamState("PS01", DateTime.UtcNow, 0);
        await fixture.TestHarness.Bus.Publish(getJamState);

        await fixture.ShouldHaveConsumedAsync<GetJamState>();

        await fixture.ShouldHavePublishedAsync<GotJamState>();
        var gotJamState = await fixture.GetFirstPublishedMessageAsync<GotJamState>();
        gotJamState.HasJam.ShouldBeFalse();
    }

    [DataRow(1, true)]
    [DataRow(0, false)]
    [DataTestMethod]
    public async Task Consume_GetJamState_JamState_RespondsExpectedValue(int state, bool expected)
    {
        var jamState = new JamState("PS01", state, DateTime.UtcNow);

        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(jamState);

        var getJamState = new GetJamState("PS01", DateTime.UtcNow, 0);
        await fixture.TestHarness.Bus.Publish(getJamState);

        await fixture.ShouldHaveConsumedAsync<GetJamState>();

        await fixture.ShouldHavePublishedAsync<GotJamState>();
        var gotJamState = await fixture.GetFirstPublishedMessageAsync<GotJamState>();
        gotJamState.HasJam.ShouldBe(expected);
    }
}