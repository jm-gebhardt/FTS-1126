using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
public class PrismatCommunicationHostOrderRepository: EfHostOrderRepository, IPrismatCommunicationHostOrderRepository
{
    /// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
    public PrismatCommunicationHostOrderRepository(PrismatCommunicationDbContext context, KaeserGraphConfiguration configuration) : base(context, configuration)
    {
    }
}