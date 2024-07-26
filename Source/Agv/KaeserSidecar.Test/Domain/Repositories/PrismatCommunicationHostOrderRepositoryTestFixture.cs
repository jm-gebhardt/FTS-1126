using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

public class PrismatCommunicationHostOrderRepositoryTestFixture : PrismatDbTestFixture
{
    public readonly IPrismatCommunicationHostOrderRepository Repository;

    public PrismatCommunicationHostOrderRepositoryTestFixture()
    {
        var context = ContextFactory.CreateDbContext();
        Repository = new PrismatCommunicationHostOrderRepository(context, OrderGenerator.KaeserConfiguration);
    }
}