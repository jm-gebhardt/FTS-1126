using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <inheritdoc />
public class StartActionService : IStartActionService
{
    private readonly IBus _bus;

    /// <summary>
    /// <inheritdoc cref="StartActionService"/>
    /// </summary>
    /// <param name="bus"></param>
    public StartActionService(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public async Task StartAction(IAgv agv, AgvAction action, string orderId, CancellationToken cancellationToken)
    {
        if (action.Nodes == null || !action.Nodes.Any())
        {
            throw new ArgumentNullException(nameof(action.Nodes));
        }

        await _bus.Publish(new StartActionCommand(agv.CorrelationId, agv.AgvId, orderId, action), cancellationToken);
    }
}