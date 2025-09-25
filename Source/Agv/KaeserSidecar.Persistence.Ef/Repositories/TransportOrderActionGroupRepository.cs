using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderActionGroupRepository"/>>
public class TransportOrderActionGroupRepository : EfActionGroupRepository, ITransportOrderActionGroupRepository
{
    /// <inheritdoc cref="ITransportOrderActionGroupRepository"/>>
    public TransportOrderActionGroupRepository(ILogger<EfActionGroupRepository> logger, IDbContextFactory<TransportOrderDbContext> dbContextFactory) : base(logger, dbContextFactory)
    {
    }
}
