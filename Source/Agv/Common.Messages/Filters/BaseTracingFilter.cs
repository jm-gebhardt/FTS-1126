using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Messages.Filters;

/// <summary>
/// Provides a base class for tracing consume/publish filters
/// </summary>
public abstract class BaseTracingFilter
{
    private readonly ILogger _logger;

    /// <inheritdoc cref="BaseTracingFilter"/>>
    protected BaseTracingFilter(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Log consume context message at information level.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    /// <param name="context">Consume context.</param>
    protected void LogInformation<T>(ConsumeContext<T> context) where T : class
    {
        _logger.LogInformation(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} Consumed {Content}", DateTime.UtcNow, context.Message);
    }

    /// <summary>
    /// Log consume context message at debug level.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    /// <param name="context">Consume context.</param>
    protected void LogDebug<T>(ConsumeContext<T> context) where T : class
    {
        _logger.LogDebug(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fffff} Consumed {Content}", DateTime.UtcNow, context.Message);
    }
}