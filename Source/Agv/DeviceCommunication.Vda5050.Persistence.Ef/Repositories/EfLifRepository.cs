using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc cref="ILifRepository"/>
public class EfLifRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory, ILogger<EfLifRepository> logger) : ILifRepository
{
    private const string LifAdditionSavePoint = "BeforeLifAddition";
   
    /// <inheritdoc cref="ILifRepository"/>
    public async Task<bool> AddFileAsync(LifFile file, ICollection<Layout> addedLayouts, ICollection<Layout> deletedLayouts)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (transaction.SupportsSavepoints)
            {
                await transaction.CreateSavepointAsync(LifAdditionSavePoint);
            }
            if (deletedLayouts.Count > 0)
            {
                HashSet<Guid> replacedIds = deletedLayouts.Select(l => l.Id).ToHashSet();
                List<Layout> trackedReplaced = await context.Set<Layout>()
                    .Where(l => replacedIds.Contains(l.Id))
                    .ToListAsync();
                context.Set<Layout>().RemoveRange(trackedReplaced);
            }
            context.Set<Layout>().AddRange(addedLayouts);
            await context.Set<LifFile>().AddAsync(file);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            logger.LogError("Adding lif {Filename}/{Id} failed, roll back transaction", file.MetaInformation.Filename, file.Id);
            if (transaction.SupportsSavepoints)
            {
                await transaction.RollbackToSavepointAsync(LifAdditionSavePoint);
                return false;
            }
            await transaction.RollbackAsync();
            return false;
        }
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

    private const string LifDeletionSavePoint = "BeforeLifDeletion";

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<bool> DeleteLifAsync(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (transaction.SupportsSavepoints)
            {
                await transaction.CreateSavepointAsync(LifDeletionSavePoint);
            }
            var file = context.Set<LifFile>().Single(l => l.Id == id);
            HashSet<Guid> layoutIds = file.LayoutIds.ToHashSet();
            IQueryable<Layout> layouts = context.Set<Layout>().Where(l => layoutIds.Contains(l.Id));
            
            context.Set<Layout>().RemoveRange(layouts);
            context.Set<LifFile>().Remove(file);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            logger.LogError("Deleting lif {Id} failed, roll back transaction", id);
            if (transaction.SupportsSavepoints)
            {
                await transaction.RollbackToSavepointAsync(LifDeletionSavePoint);
                return false;
            }
            await transaction.RollbackAsync();
            return false;
        }
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
            HashSet<Guid> replacedIds = deletedLayouts.Select(l => l.Id).ToHashSet();
            List<Layout> trackedReplaced = await context.Set<Layout>()
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
        HashSet<Guid> idSet = layoutIds.ToHashSet();
        List<Layout> layouts = await context.Set<Layout>().Where(l => idSet.Contains(l.Id)).ToListAsync();
        return layouts;
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<ICollection<LifFile>> GetAllFilesAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<LifFile>().ToListAsync();
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<(ICollection<LifFile> lifs, ICollection<Layout> layouts)> GetLifsByVehicleTypeIdsAsync(string[] vehicleTypeIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        List<LifFile> lifFiles = (await GetAllFilesAsync()).Where(f => f.SupportedVehicleTypes.Any( v=> vehicleTypeIds.Contains(v))).ToList();
        if(lifFiles.Count != 0)
        {  
            // if lif is uploaded via file upload
            ICollection<Layout> uploadedLayouts = await GetLayoutsByIdsAsync(lifFiles.SelectMany(f => f.LayoutIds).Distinct().ToList());
            return  (lifFiles, uploadedLayouts);
        }
        // if lif collection was uploaded directly
        List<Layout> layouts = await context.Set<Layout>().ToListAsync();
        return ([], layouts.Where(l => l.GetSupportedVehicleTypes().Any(v => vehicleTypeIds.Contains(v))).ToList());
    }

    /// <inheritdoc cref="ILifRepository"/>
    public async Task<ICollection<Layout>> GetLayoutsByVehicleTypeIdAsync(string vehicleTypeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        List<LifFile> lifFiles = (await GetAllFilesAsync()).Where(f => f.SupportedVehicleTypes.Contains(vehicleTypeId)).ToList();
        if (lifFiles.Count != 0)
        {
            // if lif is uploaded via file upload
            return await GetLayoutsByIdsAsync(lifFiles.SelectMany(f => f.LayoutIds).Distinct().ToList());
        }
        // if lif collection was uploaded directly
        List<Layout> layouts = await context.Set<Layout>().ToListAsync();
        return layouts.Where(l => l.GetSupportedVehicleTypes().Contains(vehicleTypeId)).ToList();
    }
}