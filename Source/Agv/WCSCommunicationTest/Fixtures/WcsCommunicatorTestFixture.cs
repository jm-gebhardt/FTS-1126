using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.WCSCommunication.Domain.Services;
using Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.WCSCommunicationTest.Fixtures;

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