using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

public class OrderRepositoryTestFixture : DbTestFixtureBase<KaeserSidecarDbContext>
{
    public readonly EfOrderRepository Repository;

    public OrderRepositoryTestFixture()
    {
        Repository = new EfOrderRepository(ContextFactory);
    }
}