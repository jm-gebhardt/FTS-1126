using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

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
    public bool IsCarryingLoad(Guid agvCorrelationId)
    {
        return _agvActionRegistry.HasUnfinishedDropActions(agvCorrelationId);
    }

    /// <inheritdoc />
    public bool CanGetNewLoad(Guid agvCorrelationId)
    {
        return !IsCarryingLoad(agvCorrelationId);
    }
}