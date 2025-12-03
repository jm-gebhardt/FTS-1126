using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Repositories;

/// <summary>
/// Represents a repository for <typeparamref name="T"/> belonging to an owner.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
/// <typeparam name="TOwnedResource">The owned resource type.</typeparam>
[PublicAPI]
public interface IOwnedResourceRepository<in T, TOwnedResource> where TOwnedResource : IOwnedResource<T>, new()
{
    /// <summary>
    /// Gets all owned <typeparamref name="T"/> for the provided owner.
    /// </summary>
    /// <param name="owner">The user to get the resources for.</param>
    Task<ICollection<TOwnedResource>> GetResourcesForOwner(string owner);

    /// <summary>
    /// Adds an owned <typeparamref name="T"/> for the owner.
    /// </summary>
    /// <param name="resource">The <typeparamref name="T"/> to add.</param>
    /// <param name="owner">The user to add the <typeparamref name="T"/> for.</param>
    /// <param name="name">The name of the owned resource instance.</param>
    Task<Guid> AddOwnedResource(T resource, string owner, string name);

    /// <summary>
    /// Updates an owned <typeparamref name="T"/>.
    /// </summary>
    /// <param name="newResource">The <typeparamref name="T"/> to update to.</param>
    /// <param name="owner">The user to update for.</param>
    /// <param name="ownedResourceId">The id of the owned resource.</param>
    /// <param name="name">The name of the owned resource instance.</param>
    Task<bool> UpdateOwnedResource(T newResource, string owner, Guid ownedResourceId, string name);

    /// <summary>
    /// Deletes an owned <typeparamref name="T"/>.
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="ownedResourceId"></param>
    Task<bool> DeleteOwnedResource(string owner, Guid ownedResourceId);
}