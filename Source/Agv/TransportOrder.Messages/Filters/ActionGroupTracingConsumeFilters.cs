using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Filters;

/// <summary>
/// Provides consume filters for messages relevant for
/// action group tracing.
/// </summary>
[UsedImplicitly]
public class ActionGroupTracingConsumeFilters: BaseTracingFilter,
    IFilter<ConsumeContext<TryStartNewActionGroup>>,
    IFilter<ConsumeContext<SelectNodesForAction>>,
    IFilter<ConsumeContext<TryStartNextActionInActionGroup>>,
    IFilter<ConsumeContext<StartAction>>,
    IFilter<ConsumeContext<CompleteAction>>,
    IFilter<ConsumeContext<CreateChargingActionGroup>>,
    IFilter<ConsumeContext<QueueActionGroup>>,
    IFilter<ConsumeContext<NodesSelectedForAction>>,
    IFilter<ConsumeContext<ActionCompleted>>,
    IFilter<ConsumeContext<NonChargingActionCompleted>>
{
    /// <inheritdoc cref="ActionGroupTracingConsumeFilters"/>>
    public ActionGroupTracingConsumeFilters(ILogger<ActionGroupTracingConsumeFilters> logger) : base (logger)
    {
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<TryStartNewActionGroup> context, IPipe<ConsumeContext<TryStartNewActionGroup>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<SelectNodesForAction> context, IPipe<ConsumeContext<SelectNodesForAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }
        
    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<TryStartNextActionInActionGroup> context, IPipe<ConsumeContext<TryStartNextActionInActionGroup>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<StartAction> context, IPipe<ConsumeContext<StartAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<CompleteAction> context, IPipe<ConsumeContext<CompleteAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<CreateChargingActionGroup> context, IPipe<ConsumeContext<CreateChargingActionGroup>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<QueueActionGroup> context, IPipe<ConsumeContext<QueueActionGroup>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<NodesSelectedForAction> context, IPipe<ConsumeContext<NodesSelectedForAction>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<ActionCompleted> context, IPipe<ConsumeContext<ActionCompleted>> next)
    {
        LogDebug(context);
        await next.Send(context);
    }

    /// <inheritdoc cref="IFilter{TContext}"/>>
    public async Task Send(ConsumeContext<NonChargingActionCompleted> context, IPipe<ConsumeContext<NonChargingActionCompleted>> next)
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