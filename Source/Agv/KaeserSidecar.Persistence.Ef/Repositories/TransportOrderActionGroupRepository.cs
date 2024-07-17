using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderActionGroupRepository"/>>
public class TransportOrderActionGroupRepository : EfActionGroupRepository, ITransportOrderActionGroupRepository
{
    /// <inheritdoc cref="ITransportOrderActionGroupRepository"/>>
    public TransportOrderActionGroupRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
