using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

public class SimulatedAgvSpawnerTestFixture : MassTransitTestFixture
{
    public const string Manufacturer = SimulatedAgvTypeConfigurationGenerator.Manufacturer;
    public const string AgvTypeIdentifier = SimulatedAgvTypeConfigurationGenerator.AgvTypeIdentifier;
    public const string FirstSerialNumber = SimulatedAgvTypeConfigurationGenerator.FirstSerialNumber;
    public const string SecondSerialNumber = SimulatedAgvTypeConfigurationGenerator.SecondSerialNumber;
    public const string ThirdSerialNumber = SimulatedAgvTypeConfigurationGenerator.ThirdSerialNumber;
    public const string FourthSerialNumber = SimulatedAgvTypeConfigurationGenerator.FourthSerialNumber;
    public const string MapId = "simulationMap";
    public const int SingleAgv = 1;
    public const int NoOfAgvs = 2;
    public const int MoreAgvs = 4;
    public const int ChangedStateUpdateIntervalMs = 4711;

    private readonly Graph _graph = GraphGenerator.Generate();

    public ISimulatedAgvSpawner Sut => Provider.GetRequiredService<ISimulatedAgvSpawner>();

    private class GetGraphResponder(Graph graph) : IConsumer<GetGraph>
    {
        public Task Consume(ConsumeContext<GetGraph> context)
        {
            return context.RespondAsync(new GotGraph(graph));
        }
    }

    private class GetDefaultMapResponder : IConsumer<GetDefaultMap>
    {
        public Task Consume(ConsumeContext<GetDefaultMap> context)
        {
            return context.RespondAsync(new GotDefaultMap(Guid.NewGuid(),
                new Map(Guid.NewGuid(), MapId, DateTime.UtcNow, string.Empty, 0, 0, 0, 0, 0)));
        }
    }

    public SimulatedAgvSpawnerTestFixture WithGraphNode()
    {
        _graph.Nodes.Add(NodeGenerator.GenerateNode());
        return this;
    }

    public new SimulatedAgvSpawnerTestFixture Build()
    {
        AddConsumers(typeof(GetGraphResponder), typeof(GetDefaultMapResponder));
        AddServices(sc =>
        {
            sc.AddLogging();
            sc.AddSingleton(_ => _graph);
            sc.AddSingleton<ISimulatedAgvSpawner, SimulatedAgvSpawner>();
        });
        base.Build();
        return this;
    }
}