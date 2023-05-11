using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

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
    /// <param name="cancellationToken"></param>
    public Task StartAction(IAgv agv, AgvAction action, CancellationToken cancellationToken);
}