using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvProviderTestFixture() : SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(TimeoutSeconds))
{
    private const double TimeoutSeconds = 6;

    private SimulatedAgvProvider _simAgvBackgroundService = null!;

    public new SimulatedAgvProviderTestFixture Build()
    {
        AddServices(s => s.AddSingleton<SimulatedAgvProvider>());

        base.Build();

        _simAgvBackgroundService = Provider.GetRequiredService<SimulatedAgvProvider>();
        return this;
    }

    public new SimulatedAgvProviderTestFixture WithSimulateOptions(Options options)
    {
        base.WithSimulateOptions(options);
        return this;
    }

    public async Task RunBackgroundService()
    {
        double waited = 0;
        var cancellationToken = new CancellationTokenSource();
        await _simAgvBackgroundService.StartAsync(cancellationToken.Token);
        while (_simAgvBackgroundService.ExecuteTask == null)
        {
            await Task.Delay(1000, cancellationToken.Token);
            waited += 1;
            if (waited >= TimeoutSeconds)
            {
                throw new Exception("Waiting for background task exceeded timeout");
            }
        }
        await _simAgvBackgroundService.ExecuteTask;
    }
}