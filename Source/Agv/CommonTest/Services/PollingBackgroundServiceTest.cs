using Gebhardt.StoreWare.Agv.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services;

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
        var service = new PollingBackgroundService<PollableTest>(Mock.Of<ILogger<PollingBackgroundService<PollableTest>>>(), new PollableTest());
        var token = new CancellationTokenSource();

        token.CancelAfter(1000); // Set timeout to avoid dead-lock in while  
        await service.StartAsync(token.Token);
        // wait for service startup
        while (service.Pollable.Counter == 0 && !token.IsCancellationRequested)
        {
        }
        token.IsCancellationRequested.ShouldBeFalse(); // Ensure that the request was not canceled using CancelAfter()

        // Stop gracefully to test that token.IsCancellationRequested is recognized 
        await service.StopAsync(token.Token);
    }
}