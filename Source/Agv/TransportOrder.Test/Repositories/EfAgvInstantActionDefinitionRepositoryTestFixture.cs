using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfAgvInstantActionDefinitionRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfAgvInstantActionDefinitionRepository Repository;

    public EfAgvInstantActionDefinitionRepositoryTestFixture()
    {
        Repository = new EfAgvInstantActionDefinitionRepository(ContextFactory);
    }
}