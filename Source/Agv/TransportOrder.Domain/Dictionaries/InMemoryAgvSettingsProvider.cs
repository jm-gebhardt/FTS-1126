using System.Collections.Concurrent;
using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;

/// <summary>
/// Provides settings for each <see cref="AgvSettingsGroup"/>.
/// </summary>
public class InMemoryAgvSettingsProvider: IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel>
{
    /// <inheritdoc />
    public ConcurrentDictionary<Guid, AgvSettingsReadModel> ConcurrentDict { get; } = new();

    /// <inheritdoc />
    public void Add(Guid key, AgvSettingsReadModel value)
    {
        ConcurrentDict.TryAdd(key, value);
    }
}