using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

namespace Gebhardt.Acc.DeviceCommunicationTest.Repositories;

public class EfActionRepositoryTestFixture: DeviceCommunicationDbTestFixture
{
    public readonly IActionRepository Repository;

    public EfActionRepositoryTestFixture()
    {
        var context = ContextFactory.CreateDbContext();
        Repository = new EfActionRepository(context);
    }
}