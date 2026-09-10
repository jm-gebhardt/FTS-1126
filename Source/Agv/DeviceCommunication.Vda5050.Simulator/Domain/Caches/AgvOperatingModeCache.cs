using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <inheritdoc cref="IAgvOperatingModeCache"/>
public class AgvOperatingModeCache : ConcurrentDictionaryProvider<Guid, AgvOperatingMode>, IAgvOperatingModeCache
{
    /// <inheritdoc cref="IAgvOperatingModeCache"/>
    public Task<AgvOperatingMode> GetOperatingModeAsync(Guid agvCorrelationId)
    {
        bool exists = ConcurrentDict.TryGetValue(agvCorrelationId, out var operatingMode);
        if (!exists)
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }
        return Task.FromResult(operatingMode);
    }

    /// <inheritdoc cref="IAgvOperatingModeCache"/>
    public Task<bool> ExistsAsync(Guid agvCorrelationId)
    {
        return Task.FromResult(ConcurrentDict.ContainsKey(agvCorrelationId));
    }

    /// <inheritdoc cref="IAgvOperatingModeCache"/>
    public Task AddOrUpdateAsync(Guid agvCorrelationId, AgvOperatingMode mode)
    {
        return Task.FromResult(ConcurrentDict.AddOrUpdate(agvCorrelationId, mode, (_, _) => mode));
    }

    /// <inheritdoc cref="IAgvOperatingModeCache"/>
    public Task RemoveAsync(Guid agvCorrelationId)
    {
        return Task.FromResult(ConcurrentDict.TryRemove(agvCorrelationId, out _));
    }
}