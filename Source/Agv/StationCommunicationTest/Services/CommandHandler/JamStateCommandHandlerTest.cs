using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

[TestClass]
public class JamStateCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateJamState_NewJamState_AddsJamState()
    {
        var jamState = new JamState("PS01".ToGuid(), 1, DateTime.UtcNow);

        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(null);

        var updateJamState = new UpdateJamState(jamState.StationId, jamState.State, jamState.LastStateChangeTime);

        await fixture.PublishMessageAsync(updateJamState);

        await fixture.ShouldHavePublishedAsync<JamStateUpdated>();
        fixture.VerifyJamStateAdded(Times.Once());
        fixture.VerifyJamStateUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateJamState_ExistingJamState_UpdatesJamState()
    {
        var jamState = new JamState("PS01".ToGuid(), 1, DateTime.UtcNow);

        var fixture = new JamStateCommandHandlerTestFixture()
            .WithJamState(jamState);

        var updateJamState = new UpdateJamState(jamState.StationId, 0, DateTime.UtcNow);

        await fixture.PublishMessageAsync(updateJamState);

        await fixture.ShouldHavePublishedAsync<JamStateUpdated>();
        fixture.VerifyJamStateAdded(Times.Never());
        fixture.VerifyJamStateUpdated(Times.Once());
    }
}