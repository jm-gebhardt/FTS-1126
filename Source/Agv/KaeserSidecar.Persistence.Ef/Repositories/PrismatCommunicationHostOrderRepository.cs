using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
public class PrismatCommunicationHostOrderRepository: EfHostOrderRepository, IPrismatCommunicationHostOrderRepository
{
    private readonly PrismatCommunicationDbContext _context;
    private readonly KaeserGraphConfiguration _configuration;

    /// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
    public PrismatCommunicationHostOrderRepository(PrismatCommunicationDbContext context, KaeserGraphConfiguration configuration) : base(context, configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
    public async Task<HostOrder?> GetNextPickOnShortLoop()
    {
        return await _context.Set<HostOrder>().Where(
                h => !h.PassedTriggerPoint
                     && h.AssignedAgv == null
                     && h.StartedDate == null
                     && !h.IsQueued
                     && h.Actions.Any(a => _configuration.PrismatAklPickTargets.Contains(a.Node) && a.ActionType == ActionType.Pick))
            .OrderBy(h => h.CreatedDate).FirstOrDefaultAsync();
    }
}