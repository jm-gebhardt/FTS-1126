using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc cref="ILifRepository"/>
public class EfLifRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : ILifRepository
{
    /// <inheritdoc cref="ILifRepository"/>
    public async Task AddFileAsync(LifFile file, ICollection<Layout> addedLayouts, ICollection<Layout> deletedLayouts)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        if (deletedLayouts.Count > 0)
        {
            var replacedIds = deletedLayouts.Select(l => l.Id).ToHashSet();
            var trackedReplaced = await context.Set<Layout>()
                .Where(l => replacedIds.Contains(l.Id))
                .ToListAsync();
            context.Set<Layout>().RemoveRange(trackedReplaced);
        }

        context.Set<Layout>().AddRange(addedLayouts);
        await context.Set<LifFile>().AddAsync(file);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<LifFile> GetFileAsync(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var file = await context.Set<LifFile>().SingleOrDefaultAsync(l => l.Id == id);
        if (file == null)
        {
            throw new LifFileNotFoundException(id);
        }
        return file;
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task DeleteLifAsync(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var file = context.Set<LifFile>().Single(l => l.Id == id);
        var layoutIds = file.LayoutIds.ToHashSet();
        var layouts = context.Set<Layout>().Where(l => layoutIds.Contains(l.Id));

        context.Set<Layout>().RemoveRange(layouts);
        context.Set<LifFile>().Remove(file);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task AddLayouts(ICollection<Layout> layouts)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.Set<Layout>().AddRangeAsync(layouts);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task UpdateLayouts(ICollection<Layout> addedLayouts, ICollection<Layout> deletedLayouts)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        if (deletedLayouts.Count > 0)
        {
            var replacedIds = deletedLayouts.Select(l => l.Id).ToHashSet();
            var trackedReplaced = await context.Set<Layout>()
                .Where(l => replacedIds.Contains(l.Id))
                .ToListAsync();
            context.Set<Layout>().RemoveRange(trackedReplaced);
        }
        context.Set<Layout>().AddRange(addedLayouts);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<ICollection<Layout>> GetLayoutsByIdsAsync(ICollection<Guid> layoutIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var idSet = layoutIds.ToHashSet();
        var layouts = await context.Set<Layout>().Where(l => idSet.Contains(l.Id)).ToListAsync();
        return layouts;
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<ICollection<LifFile>> GetAllFilesAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<LifFile>().ToListAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<bool> ExistsWithCreatorAsync(string creator)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<LifFile>()
            .AnyAsync(f => f.LifCollectionMetaInformation.Creator.ToLower().Trim() == creator.ToLower().Trim());
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<(ICollection<LifFile> lifs, ICollection<Layout> layouts)> GetLifsByVehicleTypeIdsAsync(string[] vehicleTypeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var lifFiles = (await GetAllFilesAsync()).Where(f => f.SupportedVehicleTypes.Any(v => vehicleTypeIds.Contains(v))).ToList();
        if (lifFiles.Count != 0)
        {
            // if lif is uploaded via file upload
            var uploadedLayouts = await GetLayoutsByIdsAsync(lifFiles.SelectMany(f => f.LayoutIds).Distinct().ToList());
            return (lifFiles, uploadedLayouts);
        }
        // if lif collection was uploaded directly
        var layouts = await context.Set<Layout>().ToListAsync();
        return ([], layouts.Where(l => l.GetSupportedVehicleTypes().Any(v => vehicleTypeIds.Contains(v))).ToList());
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<ICollection<Layout>> GetLayoutsByVehicleTypeIdAsync(string vehicleTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var lifFiles = (await GetAllFilesAsync()).Where(f => f.SupportedVehicleTypes.Contains(vehicleTypeId)).ToList();
        if (lifFiles.Count != 0)
        {
            // if lif is uploaded via file upload
            return await GetLayoutsByIdsAsync(lifFiles.SelectMany(f => f.LayoutIds).Distinct().ToList());
        }
        // if lif collection was uploaded directly
        var layouts = await context.Set<Layout>().ToListAsync();
        return layouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
    }
}