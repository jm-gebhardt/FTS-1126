using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfLogEntryRepositoryTestFixture : TransportOrderDbFixture
{
    public EfLogEntryRepository Repository { get; }

    public EfLogEntryRepositoryTestFixture()
    {
        Repository = new EfLogEntryRepository(ContextFactory, MappingHelper.GetMapper(new FilterableProfile(), new FilteredProfile()));
        Build();
    }
}