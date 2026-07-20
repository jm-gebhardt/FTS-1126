using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <inheritdoc cref="IAgvCache"/>
public class AgvCache: ConcurrentDictionaryProvider<Guid, AgvOperatingMode>, IAgvCache
{
    /// <inheritdoc cref="IAgvCache"/>
    public Task<AgvOperatingMode> GetOperatingModeAsync(Guid agvCorrelationId)
    {
        bool exists = ConcurrentDict.TryGetValue(agvCorrelationId, out var operatingMode);
        if (!exists)
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }
        return Task.FromResult(operatingMode);
    }

    /// <inheritdoc cref="IAgvCache"/>
    public Task<bool> ExistsAsync(Guid agvCorrelationId)
    {
        return Task.FromResult(ConcurrentDict.ContainsKey(agvCorrelationId));
    }

    /// <inheritdoc cref="IAgvCache"/>
    public Task AddOrUpdateAsync(Guid agvCorrelationId, AgvOperatingMode mode)
    {
        return Task.FromResult(ConcurrentDict.AddOrUpdate(agvCorrelationId, mode, (_, _) => mode));
    }
}