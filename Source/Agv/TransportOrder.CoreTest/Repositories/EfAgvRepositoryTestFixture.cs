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

    public EfAgvRepositoryTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
            );
        Provider = collection.BuildServiceProvider(true);
        ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
        Repository = new AgvRepository(ContextFactory);
    }

    public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        await Repository.AddAgvAsync(agv);
    }
}
