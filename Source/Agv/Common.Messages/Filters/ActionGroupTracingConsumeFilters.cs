using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Core.Observability.Abstractions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// action group tracing.
/// </summary>
/// <inheritdoc cref="ActionGroupTracingConsumeFilters"/>>
[UsedImplicitly]
public class ActionGroupTracingConsumeFilters(ILogger<ActionGroupTracingConsumeFilters> logger, ITracing tracing) : BaseTracingFilter(logger),
    IFilter<ConsumeContext<QueueActionGroup>>,
    IFilter<ConsumeContext<StartActionCommand>>,
    IFilter<ConsumeContext<UpdateActionCommand>>,
    IFilter<ConsumeContext<ActionGroupCompleted>>,
    IFilter<ConsumeContext<ActionGroupStarted>>,
    IFilter<ConsumeContext<ActionGroupFailed>>,
    IFilter<ConsumeContext<ActionAborted>>,
    IFilter<ConsumeContext<HostOrderCreated>>
{
    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<QueueActionGroup> context, IPipe<ConsumeContext<QueueActionGroup>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(QueueActionGroup), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupOrderId, context.Message.ActionGroup.OrderId), 
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupHu, context.Message.ActionGroup.HandlingUnit),
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupCorrelationId, context.Message.ActionGroup.CorrelationId),
                new KeyValuePair<string, object?>(TracingConstants.TagAgvCorrelationId, context.Message.ActionGroup.AssignedAgv)
            });

        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<StartActionCommand> context, IPipe<ConsumeContext<StartActionCommand>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(StartActionCommand), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupCorrelationId, context.Message.Action.ParentId),
                new KeyValuePair<string, object?>(TracingConstants.TagAgvCorrelationId, context.Message.CorrelationId)
            });
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<UpdateActionCommand> context, IPipe<ConsumeContext<UpdateActionCommand>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(UpdateActionCommand), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupCorrelationId, context.Message.Action.ParentId),
                new KeyValuePair<string, object?>(TracingConstants.TagAgvCorrelationId, context.Message.CorrelationId)
            });
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
    public async Task Send(ConsumeContext<ActionAborted> context, IPipe<ConsumeContext<ActionAborted>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<HostOrderCreated> context, IPipe<ConsumeContext<HostOrderCreated>> next)
    {
        using var activity = tracing.StartSpan(
            nameof(HostOrderCreated), attributes: new[]
            {
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupOrderId, context.Message.OrderDetails.OrderId),
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupHu, context.Message.Hu),
                new KeyValuePair<string, object?>(TracingConstants.TagActionGroupCorrelationId, context.Message.CorrelationId)
            });
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public void Probe(ProbeContext context)
    {
        context.CreateScope(nameof(ActionGroupTracingConsumeFilters));
    }
}