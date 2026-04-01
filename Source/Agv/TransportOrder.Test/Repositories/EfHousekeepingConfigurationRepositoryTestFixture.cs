using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfHousekeepingConfigurationRepositoryTestFixture : TransportOrderDbFixture
{
    public EfHousekeepingConfigurationRepository HousekeepingConfigurationRepository { get; }

    public EfHousekeepingConfigurationRepositoryTestFixture()
    {
        HousekeepingConfigurationRepository = new EfHousekeepingConfigurationRepository(ContextFactory);
    }

    public EfHousekeepingConfigurationRepositoryTestFixture WithConfiguration(HousekeepingConfiguration configuration)
    {
        AddInitTask(HousekeepingConfigurationRepository.AddOrUpdateAsync(configuration));
        return this;
    }

    private new void Build()
    {
        base.Build();
    }

    public new async Task<EfHousekeepingConfigurationRepositoryTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        Build();
        return this;
    }
}
