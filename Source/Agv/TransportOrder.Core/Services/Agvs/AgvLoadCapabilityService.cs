using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class AgvLoadCapabilityService : IAgvLoadCapabilityService
{
    private readonly IActionGroupRegistry _actionGroupRegistry;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvLoadCapabilityService(IActionGroupRegistry actionGroupRegistry)
    {
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <inheritdoc />
    public bool IsCarryingLoad(Guid agvCorrelationId)
    {
        return _actionGroupRegistry.HasUnfinishedDropActions(agvCorrelationId);
    }

    /// <inheritdoc />
    public bool CanGetNewLoad(Guid agvCorrelationId)
    {
        return !IsCarryingLoad(agvCorrelationId);
    }
}