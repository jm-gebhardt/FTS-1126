using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Handles the start of an AgvAction 
/// </summary>
public interface IStartActionService
{
    /// <summary>
    /// Send <see cref="StartActionCommand"/> to Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="action">The action to start</param>
    /// <param name="orderId"></param>
    /// <param name="cancellationToken"></param>
    public Task StartAction(IAgv agv, AgvAction action, string orderId, CancellationToken cancellationToken);
}