using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents the load state of an AGV. This class encapsulates whether the AGV is loaded and more information about the load.
/// </summary>
[UsedImplicitly]
public class LoadState
{
    /// <summary>
    /// Indicates whether the AGV is loaded.
    /// </summary>
    [UsedImplicitly]
    public bool IsLoaded { get; }

    // derived from Factsheet/loadSpecification
    // n:1 relation loadSets:LoadState

    /// <inheritdoc cref="LoadState"/>
    public LoadState(bool isLoaded)
    {
        IsLoaded = isLoaded;
    }
}