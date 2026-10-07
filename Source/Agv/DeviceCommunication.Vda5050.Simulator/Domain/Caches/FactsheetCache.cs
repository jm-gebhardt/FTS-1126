using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <inheritdoc cref="IFactsheetCache"/>
public class FactsheetCache : ConcurrentDictionaryProvider<Guid, string>, IFactsheetCache
{
    /// <inheritdoc cref="IFactsheetCache"/>
    public Task AddOrUpdateAsync(Guid agvTypeId, string factsheetJson)
    {
        return Task.FromResult(ConcurrentDict.AddOrUpdate(agvTypeId, factsheetJson, (_, _) => factsheetJson));
    }

    /// <inheritdoc cref="IFactsheetCache"/>
    public Task<string?> TryGetAsync(Guid agvTypeId)
    {
        ConcurrentDict.TryGetValue(agvTypeId, out string? factsheetJson);
        return Task.FromResult(factsheetJson);
    }
}