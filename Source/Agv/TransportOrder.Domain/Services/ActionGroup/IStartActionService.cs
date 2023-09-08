using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using AgvAction = TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Handles the start of an AgvAction (send to Agv)
/// </summary>
public interface IStartActionService
{
    /// <summary>
    /// Send <see cref="StartActionCommand"/> to Agv
    /// </summary>
    /// <param name="agv">The Agv to start the Action</param>
    /// <param name="action">The Action to start</param>
    /// <param name="cancellationToken"></param>
    public Task StartAction(IAgv agv, AgvAction action, CancellationToken cancellationToken);
}