using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <inheritdoc />
public class StartActionService : IStartActionService
{
    /// <inheritdoc />
    public Task StartAction(AgvAction action, CancellationToken cancellationToken)
    {
        // the ActionManager is not in use until the final switch to the new ActionManager, therefore it's deactivated
        
        // await _bus.Publish(new StartActionCommand(action.AgvCorrelationId, action), cancellationToken);
        return Task.CompletedTask;
    }
}