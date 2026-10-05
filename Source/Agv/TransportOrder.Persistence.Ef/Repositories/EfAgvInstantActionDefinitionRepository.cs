using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
[UsedImplicitly]
public class EfAgvInstantActionDefinitionRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IAgvInstantActionDefinitionRepository
{
    /// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
    public async Task<bool> Add(InstantActionDefinition instantActionDefinition)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(instantActionDefinition);
        return await context.SaveChangesAsync() > 0;
    }

    /// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
    public async Task<InstantActionDefinition?> GetInstantActionDefinition(Guid correlationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return context.Set<InstantActionDefinition>().SingleOrDefault(i => i.CorrelationId == correlationId);
    }

    /// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
    public async Task<bool> Update(InstantActionDefinition instantActionDefinition)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(instantActionDefinition);
        return await context.SaveChangesAsync() > 0;
    }

    /// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
    public async Task<ICollection<InstantActionDefinition>> GetInstantActionDefinitionsForAgv(Guid agvCorrelationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Set<InstantActionDefinition>().ToListAsync()).Where(i => i.AgvCorrelationIds.Contains(agvCorrelationId)).ToList();
    }

    /// <inheritdoc cref="IAgvInstantActionDefinitionRepository"/>>
    public async Task<Dictionary<Guid, List<InstantActionDefinition>>> GetInstantActionDefinitionsByAgv(ICollection<Guid> agvCorrelationIds)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var instantActionDefinitions = await context.Set<InstantActionDefinition>().ToListAsync();
        var instantActions = agvCorrelationIds.ToDictionary(s => s, k => instantActionDefinitions.Where(i => i.AgvCorrelationIds.Contains(k)).ToList());
        return instantActions;
    }
}