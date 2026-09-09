using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

/// <summary>
/// Represents the load state of an AGV. This class encapsulates whether the AGV is loaded and more information about the load.
/// </summary>
[UsedImplicitly]
public record LoadState
{
    /// <summary>
    /// Unique identifier for the load state.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Indicates whether the AGV is loaded.
    /// </summary>
    [UsedImplicitly]
    public bool IsLoaded { get; }

    // derived from Factsheet/loadSpecification
    // n:1 relation loadSets:LoadState

    /// <inheritdoc cref="LoadState"/>
    public LoadState(Guid id, bool isLoaded)
    {
        Id = id;
        IsLoaded = isLoaded;
    }
}