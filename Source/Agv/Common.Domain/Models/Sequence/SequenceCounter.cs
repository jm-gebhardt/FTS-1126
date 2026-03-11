using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Sequence;

/// <inheritdoc/>
[PublicAPI]
public class SequenceCounter : ISequenceCounter
{
    private uint _current;

    /// <inheritdoc/>
    public uint Current => _current;

    /// <summary>
    /// Increase the counter
    /// </summary>
    public void Increase()
    {
        Interlocked.Increment(ref _current);
    }
}

/// <inheritdoc/>
public class SequenceCounter<T> : ISequenceCounter<T> where T : class
{
    private uint _current;

    /// <inheritdoc/>
    public uint Current => _current;

    /// <summary>
    /// Increase the counter.
    /// </summary>
    public void Increase()
    {
        Interlocked.Increment(ref _current);
    }
}