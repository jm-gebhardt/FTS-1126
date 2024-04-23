using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.WCSCommunication.CmdOptions;
using Gebhardt.Acc.WCSCommunication.WcsApi.SimulationMode;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.WCSCommunicationTest.Services;

public class SimulationHostOrderFixture : PollableServiceTestFixture<SimulationHostOrderService>
{
    public SimulationHostOrderFixture()
    {
        AddServices(services =>
        {
            services.AddSingleton(_ => new SimulateOptions()
            {
                Interval = 500,
                Sources = new[] { "S01", "S02", "S03" },
                Destinations = new[] { "D01", "D02", "D03" }
            });
        });
        Build();
    }
}