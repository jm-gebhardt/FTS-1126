using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Services;

/// <summary>
/// A Polling Background Service that calls <see cref="IPollable.Poll" /> and repeats every <see cref="IPollable.PollingInterval"/>
/// </summary>
[PublicAPI]
public class PollingBackgroundService<T> : BackgroundService where T : IPollable
{
    private readonly ILogger<PollingBackgroundService<T>> _logger;

    /// <summary>
    /// The Pollable called each interval
    /// </summary>
    [PublicAPI]
    public T Pollable { get; }

    /// <summary>
    /// Polling Background Service
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="pollable">The pollable is usually injected using dependency injection</param>
    public PollingBackgroundService(ILogger<PollingBackgroundService<T>> logger, T pollable)
    {
        _logger = logger;
        Pollable = pollable;
    }

    /// <summary>
    /// Executes <see cref="IPollable.Poll" /> and waits for <see cref="IPollable.PollingInterval" /> seconds
    /// </summary>
    /// <param name="cancellationToken"></param>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogTrace("Polling {Service}", Pollable);
            try
            {
                try
                {
                    await Pollable.Poll(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred polling {Pollable}", Pollable);
                }

                await Task.Delay(Pollable.PollingInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task.Delay exited via token cancellation
                // just go to next iteration 
            }
        }
    }
}