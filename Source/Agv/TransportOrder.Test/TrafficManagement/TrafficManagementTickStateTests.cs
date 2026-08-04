using Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement;

[TestClass]
public class TrafficManagementTickStateTests
{
    private static readonly TimeSpan _minimumInterval = TimeSpan.FromMilliseconds(100);

    [TestMethod]
    public void TryMarkProcessed_FirstCall_ReturnsTrue()
    {
        var timeProvider = new FakeTimeProvider();
        var state = new TrafficManagementTickState(timeProvider);

        bool result = state.TryMarkProcessed(_minimumInterval);

        result.ShouldBeTrue();
    }

    [TestMethod]
    public void TryMarkProcessed_CalledAgainBeforeMinimumIntervalElapsed_ReturnsFalse()
    {
        var timeProvider = new FakeTimeProvider();
        var state = new TrafficManagementTickState(timeProvider);
        state.TryMarkProcessed(_minimumInterval).ShouldBeTrue();

        timeProvider.Advance(_minimumInterval - TimeSpan.FromMilliseconds(1));
        bool result = state.TryMarkProcessed(_minimumInterval);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void TryMarkProcessed_CalledAfterMinimumIntervalElapsed_ReturnsTrue()
    {
        var timeProvider = new FakeTimeProvider();
        var state = new TrafficManagementTickState(timeProvider);
        state.TryMarkProcessed(_minimumInterval).ShouldBeTrue();

        timeProvider.Advance(_minimumInterval + TimeSpan.FromMilliseconds(1));
        bool result = state.TryMarkProcessed(_minimumInterval);

        result.ShouldBeTrue();
    }
}
