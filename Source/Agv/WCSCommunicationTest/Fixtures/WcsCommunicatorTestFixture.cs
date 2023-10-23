using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

public class WcsCommunicatorTestFixture
{
    public readonly IHostCommunicator Communicator;
    public readonly IDbContextFactory<WcsHostDbContext> DbContextFactory;

    public WcsCommunicatorTestFixture()
    {
        var collection = new ServiceCollection()
            .AddDbContextFactory<WcsHostDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
            .AddTransient<IHostCommunicator, WcsDbCommunicator>();

        var provider = collection.BuildServiceProvider(true);
        Communicator = provider.GetRequiredService<IHostCommunicator>();
        DbContextFactory = provider.GetService<IDbContextFactory<WcsHostDbContext>>()!;
        EnsureDbCreated();
    }

    private void EnsureDbCreated()
    {
        using var context = DbContextFactory.CreateDbContext();
        context.Database.EnsureCreated();
    }
}