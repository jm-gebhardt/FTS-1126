using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Sequence;

/// <summary>
/// Provides an interface for typed sequence counters
/// </summary>
[PublicAPI]
public interface ISequenceCounter<T> : ISequenceCounter where T : class;

/// <summary>
/// Provides an interface for sequence counters
/// </summary>
[PublicAPI]
public interface ISequenceCounter
{
    /// <summary>
    /// Current counter value-
    /// </summary>
    public uint Current { get; }

    /// <summary>
    /// Increase the counter.
    /// </summary>
    public void Increase();
}