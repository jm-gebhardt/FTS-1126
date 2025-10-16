using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class ShortLoopWeTargetStationRepositoryTestFixture : PrismatDbTestFixture
{
    public readonly IShortLoopWeTargetStationRepository Repository;

    public ShortLoopWeTargetStationRepositoryTestFixture()
    {
        Repository = new EfShortLoopWeTargetStationRepository(ContextFactory);
    }
}