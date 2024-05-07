using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvProviderTestFixture : SimulatedAgvStateMachineTestFixture
{
    public SimulatedAgvProviderTestFixture() : base(TimeSpan.FromSeconds(6))
    {
        AddServices(s => s.AddHostedService<SimulatedAgvProvider>());
    }
}