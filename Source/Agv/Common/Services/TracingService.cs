

using Gebhardt.Core.Observability;

namespace Gebhardt.Acc.Common.Services;

/// <summary>
/// Provides a service for tracing.
/// </summary>
public class TracingService: ObservabilityBase
{
    /// <inheritdoc/>>
    public TracingService(string serviceName, string activitySourceName, string? version = null) : base(serviceName, activitySourceName, version)
    {
    }
}