using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Drives the simulation clock. Virtual time advances in fixed steps; the wall-clock rate depends on the speedup.
/// </summary>
public sealed class SimulationLoop(
    ISimulationClock clock,
    FleetHost fleet,
    IOptions<SimulationOptions> options,
    TimeProvider timeProvider,
    ILogger<SimulationLoop> logger) : BackgroundService
{
    private const int MaxStepsPerIteration = 100;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var step = TimeSpan.FromMilliseconds(Math.Max(1, options.Value.TickStepMilliseconds));
        var accumulator = TimeSpan.Zero;
        long last = timeProvider.GetTimestamp();
        logger.LogInformation("Simulation started, step {Step}, speedup {Speedup}", step, clock.Speedup);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            long now = timeProvider.GetTimestamp();
            var wallElapsed = timeProvider.GetElapsedTime(last, now);
            last = now;
            accumulator += wallElapsed * clock.Speedup;

            int steps = 0;
            while (accumulator >= step && steps < MaxStepsPerIteration && !stoppingToken.IsCancellationRequested)
            {
                clock.Advance(step);
                await fleet.AdvanceAsync(step);
                accumulator -= step;
                steps++;
            }

            if (accumulator >= step)
            {
                logger.LogWarning("Simulation cannot keep up with speedup {Speedup}, dropping {Backlog} of virtual time", clock.Speedup, accumulator);
                accumulator = TimeSpan.Zero;
            }
        }
    }
}
