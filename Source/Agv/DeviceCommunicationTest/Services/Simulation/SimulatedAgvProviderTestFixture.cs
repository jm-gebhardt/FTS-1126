using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvProviderTestFixture : SimulatedAgvStateMachineTestFixture
{
    public SimulatedAgvProviderTestFixture() : base(TimeSpan.FromSeconds(6))
    {
        AddServices(s => s.AddHostedService<SimulatedAgvProvider>());
    }
}