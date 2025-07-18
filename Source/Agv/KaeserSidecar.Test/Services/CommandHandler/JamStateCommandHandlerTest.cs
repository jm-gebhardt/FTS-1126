using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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

        await fixture.PublishMessageAsync<UpdateJamState, string>(updateJamState);

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

        await fixture.PublishMessageAsync<UpdateJamState, string>(updateJamState);

        fixture.VerifyJamStateAdded(Times.Never());
        fixture.VerifyJamStateUpdated(Times.Once());
    }
}