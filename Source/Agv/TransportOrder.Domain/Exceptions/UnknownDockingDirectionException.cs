using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raise, when an unknown docking direction is provided.
/// </summary>
public class UnknownDockingDirectionException: Exception
{
    /// <inheritdoc cref="UnknownDockingDirectionException"/>>
    public UnknownDockingDirectionException(DockingDirection? dockingDirection) : base($"Docking direction {dockingDirection} unknown"){}
}