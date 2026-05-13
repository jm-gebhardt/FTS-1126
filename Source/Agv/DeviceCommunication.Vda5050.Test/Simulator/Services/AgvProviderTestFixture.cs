using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class AgvProviderTestFixture() : SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(6))
{
    private AgvProvider _agvProvider = null!;

    public new AgvProviderTestFixture Build()
    {
        AddServices(sc => sc.AddSingleton<AgvProvider>());
        base.Build();
        _agvProvider = Provider.GetRequiredService<AgvProvider>();
        return this;
    }

    public new AgvProviderTestFixture WithOptions(Options options)
    {
        base.WithOptions(options);
        return this;
    }

    public async Task RunBackgroundService()
    {
        const double timeoutSeconds = 6;
        double waited = 0;
        using var cts = new CancellationTokenSource();
        await _agvProvider.StartAsync(cts.Token);
        while (_agvProvider.ExecuteTask == null)
        {
            await Task.Delay(1000, cts.Token);
            waited += 1;
            if (waited >= timeoutSeconds)
            {
                throw new TimeoutException("Waiting for background task exceeded timeout");
            }
        }
        await _agvProvider.ExecuteTask;
    }
}
