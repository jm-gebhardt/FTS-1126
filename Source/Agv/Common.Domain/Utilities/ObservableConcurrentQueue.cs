using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Provides an observable concurrent queue.
/// </summary>
[UsedImplicitly]
public class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    /// <summary>
    /// The even raised when an item has been enqueued.
    /// </summary>
    public event EventHandler<T>? ItemEnqueued ;
   
    /// <summary>
    /// The event raised when an item has been dequeued.
    /// </summary>
    public event EventHandler<T>? ItemDequeued ;

    /// <summary>
    /// Adds an item to the end of the <see cref=" ConcurrentQueue{T}"/>
    /// and raises the event.
    /// </summary>
    /// <param name="item">The item.</param>
    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        OnItemEnqueued(item);
    }

    /// <summary>
    /// Tries to remove the item at the beginning of the queue and raises the event.
    /// </summary>
    /// <param name="item">The item that was dequeued.</param>
    /// <returns>True if an item was removed.</returns>
    public new bool TryDequeue(out T item)
    {
        if (base.TryDequeue(out var result))
        {
            item = result!;
            OnItemDequeued(item);
            return true;
        }
        item = default!;
        return false;
    }

    /// <summary>
    /// Raises the item dequeued event.
    /// </summary>
    /// <param name="item">The item that was dequeued.</param>
    protected virtual void OnItemDequeued(T item)
    {
        ItemDequeued?.Invoke(this, item);
    }

    /// <summary>
    /// Raises the item enqueued event.
    /// </summary>
    /// <param name="item">The item that was enqueued</param>
    protected virtual void OnItemEnqueued(T item)
    {
        ItemEnqueued?.Invoke(this, item);
    }
}