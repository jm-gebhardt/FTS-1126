using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class HostOrderRepositoryTestFixture: PrismatDbTestFixture
{
    public readonly IHostOrderRepository Repository;

    public HostOrderRepositoryTestFixture()
    {
        var context = ContextFactory.CreateDbContext();
        Repository = new EfHostOrderRepository(context, ContextFactory);
    }
}