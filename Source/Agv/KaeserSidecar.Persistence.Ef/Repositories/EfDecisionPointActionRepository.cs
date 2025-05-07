using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IDecisionPointActionRepository"/>>
public class EfDecisionPointActionRepository(KaeserSidecarDbContext context): EfRepository<DecisionPointAction, KaeserSidecarDbContext>(context), IDecisionPointActionRepository
{
    private readonly KaeserSidecarDbContext _context = context;
   
    /// <inheritdoc cref="IDecisionPointActionRepository"/>>
    public async Task RemoveById(Guid actionCorrelationId)
    {
        var action = await _context.Set<DecisionPointAction>().SingleOrDefaultAsync(a => a.CorrelationId == actionCorrelationId);
        if (action != null)
        {
            _context.Remove(action);
            await _context.SaveChangesAsync();
        }
    }
    
    /// <inheritdoc cref="IDecisionPointActionRepository"/>>
    public async Task UpdateAsync(DecisionPointAction action)
    {
        _context.Update(action);
        await _context.SaveChangesAsync();
    }
}