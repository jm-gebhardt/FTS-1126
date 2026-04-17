using Shouldly;

namespace Gebhardt.Acc.CommonTest.TelegramCommunication;

[TestClass]
public class SocketCommunicatorConnectionStateTest
{
    [TestMethod]
    public void ConnectionStateChanged_NotFired_BeforeStart()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.StateChanges.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ConnectionStateChanged_Fires_True_WhenCommunicationTaskStarts()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.Sender.Start();
        await fixture.WaitForStateChangesAsync(1);

        fixture.StateChanges.Count.ShouldBe(1);
        fixture.StateChanges[0].ShouldBeTrue();

        await fixture.Sender.StopAsync();
    }

    [TestMethod]
    public async Task ConnectionStateChanged_Fires_False_DuringReconnect()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.Sender.Start();
        await fixture.WaitForStateChangesAsync(1);

        fixture.Sender.TriggerReconnect("test failure");
        await fixture.WaitForStateChangesAsync(2);

        fixture.StateChanges.Count.ShouldBeGreaterThanOrEqualTo(2);
        fixture.StateChanges[0].ShouldBeTrue();
        fixture.StateChanges[1].ShouldBeFalse();

        await fixture.Sender.StopAsync();
    }

    [TestMethod]
    public async Task ConnectionStateChanged_DoesNotFireDuplicateFalse_WhenReconnectRequestedTwice()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.Sender.Start();
        await fixture.WaitForStateChangesAsync(1);
        fixture.StateChanges[0].ShouldBeTrue();

        fixture.Sender.TriggerReconnect("first failure");
        fixture.Sender.TriggerReconnect("second failure");

        await fixture.WaitForStateChangesAsync(3);

        fixture.StateChanges.ShouldBe([true, false, true]);

        await fixture.Sender.StopAsync();
    }

    [TestMethod]
    public async Task ConnectionStateChanged_Fires_True_WhenReconnectSucceeds()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.Sender.Start();
        await fixture.WaitForStateChangesAsync(1);

        fixture.Sender.TriggerReconnect("test failure");
        await fixture.WaitForStateChangesAsync(3);

        fixture.StateChanges[0].ShouldBeTrue();
        fixture.StateChanges[1].ShouldBeFalse();
        fixture.StateChanges[2].ShouldBeTrue();

        await fixture.Sender.StopAsync();
    }

    [TestMethod]
    public async Task ConnectionStateChanged_Fires_False_WhenCommunicationTaskStops()
    {
        var fixture = new SocketCommunicatorTestFixture();

        fixture.Sender.Start();
        await fixture.WaitForStateChangesAsync(1);

        fixture.StateChanges[0].ShouldBeTrue();

        await fixture.Sender.StopAsync();
        await fixture.WaitForStateChangesAsync(2);

        fixture.StateChanges.Last().ShouldBeFalse();
    }
}