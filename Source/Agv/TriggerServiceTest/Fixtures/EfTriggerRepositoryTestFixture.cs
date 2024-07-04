using Gebhardt.Acc.TriggerService.Domain.Repositories;
using Gebhardt.Acc.TriggerService.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TriggerService.Test.Fixtures;

public class EfTriggerRepositoryTestFixture: TriggerServiceDbTestFixture
{
    public readonly ITriggerRepository Repository;

    public EfTriggerRepositoryTestFixture()
    {
        Repository = new EfTriggerRepository(ContextFactory);
    }
}