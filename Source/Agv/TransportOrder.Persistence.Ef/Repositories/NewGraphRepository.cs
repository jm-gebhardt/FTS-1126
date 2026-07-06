using System;
using System.Collections.Generic;
using System.Text;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

public class NewGraphRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<Graph, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory)
{
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

