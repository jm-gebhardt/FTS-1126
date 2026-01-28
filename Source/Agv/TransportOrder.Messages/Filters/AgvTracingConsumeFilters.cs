using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Core.Observability.Abstractions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// agv tracing.
/// </summary>
/// <inheritdoc cref="AgvTracingConsumeFilters"/>>
[UsedImplicitly]
public class AgvTracingConsumeFilters(ILogger<AgvTracingConsumeFilters> logger, ITracing tracing) : BaseTracingFilter(logger),
    IFilter<ConsumeContext<PrepareAgvForStartingAction>>,
    IFilter<ConsumeContext<HandleActionCompletion>>,
    IFilter<ConsumeContext<HandleActionAbortion>>,
    IFilter<ConsumeContext<AgvPreparedForStartingAction>>,
    IFilter<ConsumeContext<AgvBecameIdle>>,
    IFilter<ConsumeContext<AgvNeedsCharging>>,
    IFilter<ConsumeContext<AgvLastNodeIdChanged>>,
    IFilter<ConsumeContext<AgvPositionChanged>>
{
    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<PrepareAgvForStartingAction> context, IPipe<ConsumeContext<PrepareAgvForStartingAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<HandleActionCompletion> context, IPipe<ConsumeContext<HandleActionCompletion>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<HandleActionAbortion> context, IPipe<ConsumeContext<HandleActionAbortion>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc />
    public async Task Send(ConsumeContext<AgvPreparedForStartingAction> context, IPipe<ConsumeContext<AgvPreparedForStartingAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc />
    public async Task Send(ConsumeContext<AgvNeedsCharging> context, IPipe<ConsumeContext<AgvNeedsCharging>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc />
    public async Task Send(ConsumeContext<AgvBecameIdle> context, IPipe<ConsumeContext<AgvBecameIdle>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(AgvBecameIdle), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagAgvCorrelationId, context.Message.CorrelationId)
            });
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<AgvLastNodeIdChanged> context, IPipe<ConsumeContext<AgvLastNodeIdChanged>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<AgvPositionChanged> context, IPipe<ConsumeContext<AgvPositionChanged>> next)
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