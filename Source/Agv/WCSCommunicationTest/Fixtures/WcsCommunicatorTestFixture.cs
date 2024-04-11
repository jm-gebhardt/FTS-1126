using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Fixtures;

public class WcsCommunicatorTestFixture : DbTestFixtureBase<WcsHostDbContext>
{
    public IHostCommunicator Communicator { get; }

    public WcsCommunicatorTestFixture()
    {
        AddServices(s => s.AddTransient<IHostCommunicator, WcsDbCommunicator>());

        Build();
        Communicator = Provider.GetRequiredService<IHostCommunicator>();
    }
}