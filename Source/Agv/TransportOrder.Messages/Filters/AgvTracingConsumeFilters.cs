using Gebhardt.StoreWare.Agv.Common.Messages.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// agv tracing.
/// </summary>
[UsedImplicitly]
public class AgvTracingConsumeFilters : BaseTracingFilter,
    IFilter<ConsumeContext<PrepareAgvForStartingAction>>,
    IFilter<ConsumeContext<HandleActionCompletion>>,
    IFilter<ConsumeContext<AgvPreparedForStartingAction>>,
    IFilter<ConsumeContext<AgvBecameIdle>>,
    IFilter<ConsumeContext<AgvNeedsCharging>>,
    IFilter<ConsumeContext<AgvLastNodeIdChanged>>
{
    /// <inheritdoc cref="AgvTracingConsumeFilters"/>>
    public AgvTracingConsumeFilters(ILogger<AgvTracingConsumeFilters> logger) : base(logger)
    {
    }

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
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(AgvTracingConsumeFilters));
    }
}