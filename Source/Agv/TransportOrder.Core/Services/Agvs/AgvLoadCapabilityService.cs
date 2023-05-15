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
    public Task<bool> IsCarryingLoadAsync(Guid agvCorrelationId)
    {
        return _actionGroupRegistry.HasUnfinishedDropActionsAsync(agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<bool> CanGetNewLoadAsync(Guid agvCorrelationId)
    {
        return !await IsCarryingLoadAsync(agvCorrelationId);
    }
}