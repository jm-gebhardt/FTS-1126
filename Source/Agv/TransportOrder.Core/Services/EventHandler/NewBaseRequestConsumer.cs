using Gebhardt.Acc.Common.Messages.ActionGroups.Vehicle;
using Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>Consumes new-base requests from AGVs and delegates to <see cref="ITrafficManager"/>.</summary>
[UsedImplicitly]
public class NewBaseRequestConsumer(ITrafficManager trafficManager) : IConsumer<AgvNewBaseRequested>
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<AgvNewBaseRequested> context)
    {
        return trafficManager.HandleNewBaseRequestAsync(context.Message.CorrelationId, context.CancellationToken);
    }
}