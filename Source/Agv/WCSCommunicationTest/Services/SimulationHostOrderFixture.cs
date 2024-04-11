using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.SimulationMode;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Services;

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