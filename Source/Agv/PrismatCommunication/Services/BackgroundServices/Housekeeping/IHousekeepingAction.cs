using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices.Housekeeping;

/// <summary>
/// Performs the housekeeping delete for a specific entity type.
/// </summary>
public interface IHousekeepingAction
{
    /// <summary>The entity type this action handles.</summary>
    HousekeepingEntityType EntityType { get; }

    /// <summary>Delete all entries of the entity type that are older than <paramref name="outdatedTime"/>.</summary>
    [PublicAPI]
    Task ExecuteAsync(DateTime outdatedTime, CancellationToken cancellationToken = default);
}
