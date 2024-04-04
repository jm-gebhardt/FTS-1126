using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class EfAgvRepositoryTestFixture
{
    public AgvRepository Repository { get; }

    private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

    private ServiceProvider Provider { get; }

    private readonly IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> _inMemorySettingsProvider = new InMemoryAgvSettingsProvider();

    public EfAgvRepositoryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
            );
        Provider = collection.BuildServiceProvider(true);
        ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
        Repository = new AgvRepository(ContextFactory, _inMemorySettingsProvider);
    }

    public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _inMemorySettingsProvider.Add(agv.AgvSettingsGroup, EfAgvSettingsProvider.DefaultSettings);
        await Repository.AddAgvAsync(agv);
    }
}
