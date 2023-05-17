using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <inheritdoc />
public class StartActionService : IStartActionService
{
    private readonly IBus _bus;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bus"></param>
    public StartActionService(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public async Task StartAction(AgvAction action, CancellationToken cancellationToken)
    {
        if (action.AssignedAgv == null)
        {
            throw new AssignedAgvNotFoundException(action);
        }

        if (action.Nodes == null || !action.Nodes.Any())
        {
            throw new ArgumentNullException(nameof(action.Nodes));
        }

        var assignedAgv = (Guid) action.AssignedAgv;
        await _bus.Publish(new StartActionCommand(assignedAgv, action), cancellationToken);
    }
}