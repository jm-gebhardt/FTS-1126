using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Extensions;

/// <summary>
/// Provides extension method for <see cref="DockingDirection"/>
/// </summary>
public static class DockingDirectionExtension
{
    /// <summary>
    /// Get the dock front, considering the given dock front and the docking direction.
    /// </summary>
    /// <param name="dockingDirection">The docking direction.</param>
    /// <param name="dockFront">A given dock front value</param>
    /// <returns>A dock front value.</returns>
    public static bool GetDockfront(this DockingDirection? dockingDirection, bool dockFront)
    {
        return dockingDirection switch
        {
            null or DockingDirection.Both => dockFront,
            DockingDirection.Front => true,
            DockingDirection.Back => false,
            _ => throw new UnknownDockingDirectionException(dockingDirection)
        };
    }
}