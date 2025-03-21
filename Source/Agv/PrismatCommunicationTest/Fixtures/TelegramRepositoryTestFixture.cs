using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class TelegramRepositoryTestFixture: PrismatDbTestFixture
{
    public readonly ITelegramRepository Repository;

    public TelegramRepositoryTestFixture()
    {
        Repository = new EfTelegramRepository(ContextFactory);
    }
}