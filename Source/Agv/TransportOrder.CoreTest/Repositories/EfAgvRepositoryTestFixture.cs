using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

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