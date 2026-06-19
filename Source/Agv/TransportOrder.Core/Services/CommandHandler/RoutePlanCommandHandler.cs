using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Maintains the in-memory route plan cache in response to dispatch commands and action group lifecycle events.
/// </summary>
[UsedImplicitly]
public class RoutePlanCommandHandler(IAgvRoutePlanCacheProvider routePlanCache) :
    IConsumer<DispatchRouteCommand>,
    IConsumer<UpdateDispatchedRouteCommand>,
    IConsumer<AbortAndDispatchRouteCommand>,
    IConsumer<ResendDispatchedRouteCommand>,
    IConsumer<ActionGroupCompleted>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<DispatchRouteCommand> context)
    {
        routePlanCache.ConcurrentDict[context.Message.CorrelationId] = context.Message.Route;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<UpdateDispatchedRouteCommand> context)
    {
        routePlanCache.ConcurrentDict[context.Message.CorrelationId] = context.Message.Route;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AbortAndDispatchRouteCommand> context)
    {
        routePlanCache.ConcurrentDict[context.Message.CorrelationId] = context.Message.Route;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ResendDispatchedRouteCommand> context)
    {
        routePlanCache.ConcurrentDict[context.Message.CorrelationId] = context.Message.Route;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        if (context.Message.ActionGroup.AssignedAgv == null)
        {
            return Task.CompletedTask;
        }

        routePlanCache.ConcurrentDict.TryRemove(context.Message.ActionGroup.AssignedAgv.Value, out _);
        return Task.CompletedTask;
    }
}
