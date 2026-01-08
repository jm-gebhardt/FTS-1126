using Gebhardt.Acc.Common.Domain.Utilities;
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
}