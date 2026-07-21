using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <inheritdoc cref="IAgvStateCache"/>
public class AgvStateCache : ConcurrentDictionaryProvider<Guid, MqttState>, IAgvStateCache
{
    /// <inheritdoc cref="IAgvStateCache"/>
    public Task AddOrUpdateAsync(Guid agvCorrelationId, MqttState state)
    {
        return Task.FromResult(ConcurrentDict.AddOrUpdate(agvCorrelationId, state, (_, _) => state));
    }

    /// <inheritdoc cref="IAgvStateCache"/>
    public Task<MqttState?> TryGetAsync(Guid agvCorrelationId)
    {
        ConcurrentDict.TryGetValue(agvCorrelationId, out var state);
        return Task.FromResult(state);
    }
}