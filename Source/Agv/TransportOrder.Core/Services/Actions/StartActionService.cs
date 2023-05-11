using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <inheritdoc />
public class StartActionService : IStartActionService
{
    private readonly IBus _bus;

    private readonly IAgvRegistry _agvRegistry;

    /// <summary>
    /// <inheritdoc cref="StartActionService"/>
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="agvRegistry"></param>
    public StartActionService(IBus bus, IAgvRegistry agvRegistry)
    {
        _bus = bus;
        _agvRegistry = agvRegistry;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public async Task StartAction(IAgv agv, AgvAction action, CancellationToken cancellationToken)
    {
        if (action.Nodes == null || !action.Nodes.Any())
        {
            throw new ArgumentNullException(nameof(action.Nodes));
        }
        
        await _bus.Publish(new StartActionCommand(agv.CorrelationId, agv.AgvId, action), cancellationToken);
    }
}