using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Consumes AGV action commands and dispatch <see cref="RoutePlan"/> via <see cref="ITrafficManager"/>.
/// </summary>
[UsedImplicitly]
public class ActionDispatchConsumer(ITrafficManager trafficManager) :
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<AbortAndStartActionCommand>,
    IConsumer<ResendAction>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        return trafficManager.AbortAndDispatchRouteAsync(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.Action, context.Message.AbortedActionCorrelationId, context.CancellationToken);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<ResendAction> context)
    {
        return trafficManager.ResendRouteAsync(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.Action, context.CancellationToken);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<StartActionCommand> context)
    {
        return trafficManager.AddNewRouteAsync(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.Action, context.CancellationToken);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<UpdateActionCommand> context)
    {
        return trafficManager.UpdateRouteAsync(context.Message.CorrelationId, context.Message.SerialNumber, context.Message.Action, context.CancellationToken);
    }
}