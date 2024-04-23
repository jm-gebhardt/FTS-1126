using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// action group tracing.
/// </summary>
[UsedImplicitly]
public class ActionGroupTracingConsumeFilters : BaseTracingFilter,
    IFilter<ConsumeContext<QueueActionGroup>>,
    IFilter<ConsumeContext<StartActionCommand>>,
    IFilter<ConsumeContext<ActionGroupCompleted>>,
    IFilter<ConsumeContext<ActionGroupStarted>>,
    IFilter<ConsumeContext<ActionGroupFailed>>
{
    /// <inheritdoc cref="ActionGroupTracingConsumeFilters"/>>
    public ActionGroupTracingConsumeFilters(ILogger<ActionGroupTracingConsumeFilters> logger) : base(logger)
    {
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<QueueActionGroup> context, IPipe<ConsumeContext<QueueActionGroup>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<StartActionCommand> context, IPipe<ConsumeContext<StartActionCommand>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<ActionGroupCompleted> context, IPipe<ConsumeContext<ActionGroupCompleted>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<ActionGroupStarted> context, IPipe<ConsumeContext<ActionGroupStarted>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<ActionGroupFailed> context, IPipe<ConsumeContext<ActionGroupFailed>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(ActionGroupTracingConsumeFilters));
    }
}