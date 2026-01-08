using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Runtime.CompilerServices;

namespace Gebhardt.Acc.OrderTranslator.Domain.Services;

/// <summary>
/// Provides a startup health check
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    /// <summary>
    /// True, if the startup is complete.
    /// </summary>
    public bool StartupCompleted
    {
        get => Interlocked.CompareExchange(ref Unsafe.As<bool, int>(ref field), 0, 0) == 1;
        set => Interlocked.Exchange(ref Unsafe.As<bool, int>(ref field), value ? 1 : 0);
    }

    /// <inheritdoc/>>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (StartupCompleted)
        {
            return Task.FromResult(HealthCheckResult.Healthy("The startup task has completed."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("That startup task is still running."));
    }
}