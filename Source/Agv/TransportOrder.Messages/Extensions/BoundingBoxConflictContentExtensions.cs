using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Extensions;

/// <summary>
/// Extensions for <see cref="BoundingBoxConflictContent"/>.
/// </summary>
public static class BoundingBoxConflictContentExtensions
{
    /// <summary>
    /// Returns the ID of the resource on the other side of each conflict pair.
    /// </summary>
    /// <param name="conflicts">The conflict pairs.</param>
    /// <param name="ownResourceId">The resource whose conflict partners are wanted.</param>
    public static IReadOnlyList<Guid> ConflictingResourceIds(this IEnumerable<BoundingBoxConflictContent> conflicts, Guid ownResourceId)
    {
        return conflicts
            .Select(c => c.ResourceAId == ownResourceId ? c.ResourceBId : c.ResourceAId)
            .ToList();
    }
}