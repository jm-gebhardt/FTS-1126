using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// 
/// </summary>
/// <param name="contextFactory"></param>
[UsedImplicitly]
public class NewGraphRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<Graph, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [UsedImplicitly]
    public async Task<Graph?> GetGraphById(Guid id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var graph = await context.Set<Graph>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.Stations)
            .SingleOrDefaultAsync(g => g.Id == id);
        return graph;
    }
}

