using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Extensions for collection types.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Add range to set (similar to <see cref="List{T}.AddRange(IEnumerable{T})"/>).
    /// </summary>
    /// <param name="set"></param>
    /// <param name="range"></param>
    /// <returns>Returns true when all elements from range had been added, returns false when there were any duplicates.</returns>
    [PublicAPI]
    public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> range)
    {
        bool allAdded = true;
        foreach (var item in range)
        {
            allAdded &= set.Add(item);
        }
        return allAdded;
    }

    /// <summary>
    /// Adds all the key value pairs to the dictionary that do not already exist.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dictionary">The dictionary to extend.</param>
    /// <param name="kvEnumerable">The key-value enumerable to add to the dictionary.</param>
    [PublicAPI]
    public static bool AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> kvEnumerable) where TKey: notnull
    {
        bool allAdded = true;
        foreach (var kv in kvEnumerable)
        {
            if (dictionary.ContainsKey(kv.Key))
            {
                allAdded = false;
            } 
            else
            {
                dictionary.Add(kv.Key, kv.Value);
            }
        }
        return allAdded;
    }
}