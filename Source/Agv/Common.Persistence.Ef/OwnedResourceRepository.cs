using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <inheritdoc/>
public class OwnedResourceRepository<T, TOwnedResource, TDbContext>(IDbContextFactory<TDbContext> contextFactory) : IOwnedResourceRepository<T, TOwnedResource> where TDbContext : DbContext where TOwnedResource : class, IOwnedResource<T>, new()
{
    /// <inheritdoc/>
    public async Task<ICollection<TOwnedResource>> GetResourcesForOwner(string owner)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var result = await context.Set<TOwnedResource>().Where(o => o.Owner == owner).ToListAsync();

        return result;
    }

    /// <inheritdoc/>
    public async Task<ICollection<TOwnedResource>> GetAllResources()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var result = await context.Set<TOwnedResource>().ToListAsync();

        return result;
    }

    /// <inheritdoc/>
    public async Task<Guid> AddOwnedResource(T resource, string owner, string name)
    {
        IOwnedResource<T> ownedResource = new TOwnedResource { Resource = resource, Owner = owner, ResourceCorrelationId = Guid.NewGuid(), ResourceName = name };

        await using var context = await contextFactory.CreateDbContextAsync();

        var result = context.Add(ownedResource);

        await context.SaveChangesAsync();

        return result.Entity.ResourceCorrelationId;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateOwnedResource(T newResource, string owner, Guid ownedResourceId, string name)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            var resource = await context.Set<TOwnedResource>().SingleAsync(f => f.Owner == owner && f.ResourceCorrelationId == ownedResourceId);

            resource.Resource = newResource;
            resource.ResourceName = name;

            int result = await context.SaveChangesAsync();

            return result > 0;
        }
        catch (InvalidOperationException)
        {
            throw new OwnedResourceNotFoundException(typeof(T).Name);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteOwnedResource(string owner, Guid ownedResourceId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        try {
            var resource = await context.Set<TOwnedResource>().SingleAsync(f => f.Owner == owner && f.ResourceCorrelationId == ownedResourceId);

            context.Remove(resource);

            int result = await context.SaveChangesAsync();

            return result > 0;
        }
        catch (InvalidOperationException)
        {
            throw new OwnedResourceNotFoundException(typeof(T).Name);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteResource(Guid ownedResourceId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            var resource = await context.Set<TOwnedResource>().SingleAsync(f => f.ResourceCorrelationId == ownedResourceId);

            context.Remove(resource);

            int result = await context.SaveChangesAsync();

            return result > 0;
        }
        catch (InvalidOperationException)
        {
            throw new OwnedResourceNotFoundException(typeof(T).Name);
        }
    }
}