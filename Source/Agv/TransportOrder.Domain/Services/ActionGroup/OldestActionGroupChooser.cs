using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Selects next action based on which action group is the oldest one.
/// </summary>
public class OldestActionGroupChooser: BaseChooseAgvActionGroupService
{
    /// <inheritdoc cref="OldestActionGroupChooser"/>>
    public OldestActionGroupChooser(
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        ILogger<OldestActionGroupChooser> logger
    ) : base(actionGroupRepository, nodeStateRepository, logger)
    {
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
    {
        return Task.FromResult(notStartedActionGroups.OrderBy(a => a.CreatedDate).FirstOrDefault());
    }
}