using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.StationCommunication.Test.Repositories;

public class OrderRepositoryTestFixture : DbTestFixtureBase<StationCommunicationDbContext>
{
    public readonly EfOrderRepository Repository;

    public OrderRepositoryTestFixture()
    {
        Repository = new EfOrderRepository(ContextFactory);
    }
}