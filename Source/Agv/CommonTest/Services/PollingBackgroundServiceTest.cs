using Gebhardt.Acc.Common.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Services;

/// <summary>
/// Test implementation
/// </summary>
public class PollableTest : IPollable
{
    /// <summary>
    /// IPollable test implementation that increments <see cref="Counter"/> on each call.
    /// </summary>
    public int Counter;

    /// <summary>
    /// Increment Counter on each poll
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Poll(CancellationToken cancellationToken = default)
    {
        Counter++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// PollingBackgroundService Tests
/// </summary>
[TestClass]
public class PollingBackgroundServiceTest
{
    /// <summary>
    /// Start and stop service
    /// </summary>
    [TestMethod]
    public async Task Service_start_and_stopped()
    {
        var service = new PollingBackgroundService<PollableTest>(
            Mock.Of<ILogger<PollingBackgroundService<PollableTest>>>(),
            new PollableTest());

        var startCts = new CancellationTokenSource();

        // Start the service
        await service.StartAsync(startCts.Token);

        // Wait for the service to poll at least once (with reasonable timeout)
        var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (service.Pollable.Counter == 0 && !timeoutCts.Token.IsCancellationRequested)
        {
            await Task.Delay(50, timeoutCts.Token).ConfigureAwait(false); // Small delay to avoid tight spin
        }

        // Verify the service successfully polled before timeout
        service.Pollable.Counter.ShouldBeGreaterThan(0, "Service should have polled at least once");

        // Stop the service gracefully with a fresh cancellation token
        var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StopAsync(stopCts.Token);

        // Verify graceful shutdown completed without cancellation
        stopCts.Token.IsCancellationRequested.ShouldBeFalse("Service should stop gracefully");
    }
}