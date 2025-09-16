using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// Provides a mapping of trigger point names to node ids.
/// </summary>
public class InMemoryTriggerPointMapping : IConcurrentDictionaryProvider<string, Guid>
{
    /// <inheritdoc />>
    public ConcurrentDictionary<string, Guid> ConcurrentDict { get; } = new();

    /// <inheritdoc />>
    public void Add(string key, Guid value)
    {
        ConcurrentDict.AddOrUpdate(key, value, (_, _) => value);
    }
}