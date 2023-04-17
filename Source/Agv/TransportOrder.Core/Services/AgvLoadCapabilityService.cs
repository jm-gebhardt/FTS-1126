using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <inheritdoc />
public class AgvLoadCapabilityService : IAgvLoadCapabilityService
{
    private readonly IAgvActionRegistry _agvActionRegistry;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvLoadCapabilityService(IAgvActionRegistry agvActionRegistry)
    {
        _agvActionRegistry = agvActionRegistry;
    }

    /// <inheritdoc />
    public bool IsCarryingLoad(string agvId)
    {
        return _agvActionRegistry.GetUnfinishedActions(agvId).Any(a => a.ActionType is ActionType.Pick or ActionType.Drop);
    }

    /// <inheritdoc />
    public bool CanGetNewLoad(string agvId)
    {
        return !IsCarryingLoad(agvId);
    }
}