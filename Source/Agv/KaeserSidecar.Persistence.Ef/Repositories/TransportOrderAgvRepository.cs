using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderAgvRepository"/>>
public class TransportOrderAgvRepository : AgvRepository, ITransportOrderAgvRepository
{
    /// <inheritdoc cref="ITransportOrderAgvRepository"/>>
    public TransportOrderAgvRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : base(dbContextFactory) { }
}
