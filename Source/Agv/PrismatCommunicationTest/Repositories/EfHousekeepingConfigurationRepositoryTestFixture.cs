using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

namespace Gebhardt.Acc.PrismatCommunicationTest.Repositories;

public class EfHousekeepingConfigurationRepositoryTestFixture : PrismatDbTestFixture
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
