using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Responsible for choosing the best Agv for an ActionGroup and also to choose the best ActionGroup for an Agv
/// </summary>
public interface IChooseAgvActionGroupService
{
    /// <summary>
    /// Get not started ActionGroups, returns the best action in the first ActionGroup that Agv can handle all actions
    /// </summary>
    /// <exception cref="AgvLocationUnknownException"></exception>
    Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Models.Agv.Agv agv, AgvData? agvData, CancellationToken cancellationToken);
}