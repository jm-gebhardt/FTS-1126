using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfAgvRepositoryTestFixture : TransportOrderDbFixture
{
    public AgvRepository Repository { get; }

    private readonly IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> _inMemorySettingsProvider = new InMemoryAgvSettingsProvider();

    public EfAgvRepositoryTestFixture()
    {
        Repository = new AgvRepository(ContextFactory, _inMemorySettingsProvider);
    }

    public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _inMemorySettingsProvider.Add(agv.AgvSettingsGroup, EfAgvSettingsProvider.DefaultSettings);
        await Repository.AddAgvAsync(agv);
    }
}