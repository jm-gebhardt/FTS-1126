using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Core.Observability.Abstractions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// agv tracing.
/// </summary>
/// <inheritdoc cref="AgvTracingConsumeFilters"/>>
[UsedImplicitly]
public class AgvTracingConsumeFilters(ILogger<AgvTracingConsumeFilters> logger, ITracing tracing) : BaseTracingFilter(logger),
    IFilter<ConsumeContext<RequestAgvGraph>>,
    IFilter<ConsumeContext<AgvErrorStatusReceived>>
{
    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<RequestAgvGraph> context, IPipe<ConsumeContext<RequestAgvGraph>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(RequestAgvGraph), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagAgvCorrelationId, context.Message.CorrelationId)
            });
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<AgvErrorStatusReceived> context, IPipe<ConsumeContext<AgvErrorStatusReceived>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(AgvTracingConsumeFilters));
    }
}