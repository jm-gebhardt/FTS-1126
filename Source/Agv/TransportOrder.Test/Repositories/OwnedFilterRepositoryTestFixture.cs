using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class OwnedFilterRepositoryTestFixture : TransportOrderDbFixture
{
    public OwnedFilterRepository FilterRepository { get; }

    public OwnedFilterRepositoryTestFixture()
    {
        FilterRepository = new OwnedFilterRepository(ContextFactory);
    }

    public new async Task<OwnedFilterRepositoryTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        return this;
    }
}