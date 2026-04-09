using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfBackgroundEditorRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfBackgroundEditorRepository Repository;

    public EfBackgroundEditorRepositoryTestFixture()
    {
        Repository = new EfBackgroundEditorRepository(ContextFactory);
    }
}
