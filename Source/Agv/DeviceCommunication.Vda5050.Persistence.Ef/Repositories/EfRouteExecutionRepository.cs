using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc cref="IRouteExecutionRepository"/>
public class EfRouteExecutionRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory) : IRouteExecutionRepository
{
    /// <inheritdoc/>
    public async Task<RouteExecution?> GetAsync(Guid routeId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<RouteExecution>().AsNoTracking().SingleOrDefaultAsync(r => r.RouteId == routeId);
    }

    /// <inheritdoc/>
    public async Task<RouteExecution> GetRequiredAsync(Guid routeId)
    {
        return await GetAsync(routeId) ?? throw new RouteExecutionNotFoundException(routeId);
    }

    /// <inheritdoc/>
    public async Task<RouteExecution?> GetActiveByAgvCorrelationIdAsync(Guid agvCorrelationId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<RouteExecution>()
            .SingleOrDefaultAsync(r => r.AgvCorrelationId == agvCorrelationId && r.LifecycleStatus == RouteExecutionStatus.Active);
    }

    /// <inheritdoc/>
    public async Task<RouteExecution?> GetByCurrentOrderIdAsync(Guid orderId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Set<RouteExecution>().AsNoTracking().SingleOrDefaultAsync(r => r.CurrentOrderId == orderId);
    }

    /// <inheritdoc/>
    public async Task AddAsync(RouteExecution routeExecution)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Add(routeExecution);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(RouteExecution routeExecution)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var existing = await context.Set<RouteExecution>()
            .Include(e => e.Steps) 
            .FirstAsync(e => e.RouteId == routeExecution.RouteId);

        context.Entry(existing).CurrentValues.SetValues(routeExecution);

        existing.Steps.Clear();
        foreach (var step in routeExecution.Steps)
        {
            existing.Steps.Add(step);
        }

        await context.SaveChangesAsync();
    }
}
