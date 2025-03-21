using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

public class EfTelegramRepositoryTestFixture<TTelegram> : DbTestFixtureBase<KaeserSidecarDbContext>
where TTelegram: Telegram
{
    public readonly EfTelegramRepository<TTelegram> Repository;

    public EfTelegramRepositoryTestFixture()
    {
        Repository = new EfTelegramRepository<TTelegram>(ContextFactory);
    }
}