using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class AgvProviderTestFixture() : SimulatedAgvStateMachineTestFixture(TimeSpan.FromSeconds(6))
{
    private readonly List<SimulatedAgvTypeConfiguration> _configurations = [];

    private AgvProvider _agvProvider = null!;

    private class GetGraphResponder : IConsumer<GetGraph>
    {
        public Task Consume(ConsumeContext<GetGraph> context)
        {
            List<Node> nodes = [new(Guid.NewGuid(), "", "", 0, 0, "")];
            return context.RespondAsync(new GotGraph(new Graph(nodes, [], [])));
        }
    }
    
    private class FakeSimulatedAgvTypeConfigurationRepository(IReadOnlyCollection<SimulatedAgvTypeConfiguration> configurations)
        : ISimulatedAgvTypeConfigurationRepository
    {
        public Task<SimulatedAgvTypeConfiguration?> GetAsync(Guid agvTypeId)
        {
            return Task.FromResult(configurations.SingleOrDefault(c => c.Id == agvTypeId));
        }

        public Task<IReadOnlyCollection<SimulatedAgvTypeConfiguration>> GetAllAsync()
        {
            return Task.FromResult(configurations);
        }

        public Task AddAsync(SimulatedAgvTypeConfiguration configuration)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SimulatedAgvTypeConfiguration configuration)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid agvTypeId)
        {
            return Task.CompletedTask;
        }
    }

    public AgvProviderTestFixture WithConfiguration(SimulatedAgvTypeConfiguration configuration)
    {
        _configurations.Add(configuration);
        return this;
    }

    public new AgvProviderTestFixture Build()
    {
        AddConsumers(typeof(GetGraphResponder));
        AddServices(sc =>
        {
            sc.AddSingleton<ISimulatedAgvTypeConfigurationRepository>(_ => new FakeSimulatedAgvTypeConfigurationRepository(_configurations));
            sc.AddSingleton<ISimulatedAgvSpawner, SimulatedAgvSpawner>();
            sc.AddSingleton<AgvProvider>();
        });
        base.Build();
        _agvProvider = Provider.GetRequiredService<AgvProvider>();
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