using System.Collections;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// A stack that can remove arbitrary items by identity.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class RandomAccessStack<T> : ICollection<T> where T : IEquatable<T>
{
    private readonly List<T> _backing;

    /// <inheritdoc cref="RandomAccessStack{T}"/>
    [JsonConstructor]
    public RandomAccessStack() : this([])
    {
    }

    /// <inheritdoc cref="RandomAccessStack{T}"/>
    private RandomAccessStack(T[] initial)
    {
        _backing = initial.ToList();
    }

    /// <summary>
    /// Pushes the provided item into the <see cref="RandomAccessStack{T}"/>
    /// </summary>
    /// <param name="item"></param>
    public void Push(T item)
    {
        _backing.Remove(item);
        _backing.Insert(0, item);
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        _backing.CopyTo(array, arrayIndex);
    }

    bool ICollection<T>.Remove(T item)
    {
        return _backing.Remove(item);
    }

    /// <inheritdoc />
    public int Count => _backing.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <summary>
    /// Removes an item from the <see cref="RandomAccessStack{T}"/>
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void Remove(T item)
    {
        _backing.RemoveAll(item.Equals);
    }

    /// <summary>
    /// Checks if the first item occurs before the second. Items not in the <see cref="RandomAccessStack{T}"/> are always considered to be before items that are.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns>True if the first item comes before the second, false otherwise; Also false if neither are in the <see cref="RandomAccessStack{T}"/></returns>
    public bool IsBefore(T first, T second)
    {
        return _backing.IndexOf(first) < _backing.IndexOf(second);
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        _backing.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _backing.Clear();
    }

    /// <summary>
    /// Returns all the items after the provided item.
    /// </summary>
    /// <param name="item">The item to find.</param>
    public IEnumerable<T> After(T item)
    {
        int index = _backing.IndexOf(item);

        return _backing.Skip(index + 1);
    }

    /// <summary>
    /// Checks if an item is contained in this <see cref="RandomAccessStack{T}"/>.
    /// </summary>
    /// <param name="item">The item to check.</param>
    public bool Contains(T item)
    {
        return _backing.Contains(item);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return _backing.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_backing).GetEnumerator();
    }
}