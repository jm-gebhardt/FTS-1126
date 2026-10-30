using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Messages.NewGraph.Requests;
using Gebhardt.Acc.Common.Testing.Fixtures;
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
    public const string MapId = "map";
    public const int SingleAgv = 1;
    public const int NoOfAgvs = 2;
    public const int MoreAgvs = 4;
    public const int ChangedStateUpdateIntervalMs = 4711;

    private Graph _graph = new([], [], []);

    public ISimulatedAgvSpawner Sut => Provider.GetRequiredService<ISimulatedAgvSpawner>();
   
    private class GetGraphResponder(Graph graph) : IConsumer<GetGraph>
    {
        public Task Consume(ConsumeContext<GetGraph> context)
        {
            return context.RespondAsync(new GotGraph(graph));
        }
    }

    public SimulatedAgvSpawnerTestFixture WithGraphNode()
    {
        var nodes = _graph.Nodes.ToList();
        nodes.Add(new Node(Guid.NewGuid(), "Gebhardt.R1", "R1", "R1",0,0, "", ""));
        _graph = new Graph(nodes, _graph.Edges, _graph.Stations);
        return this;
    }

    public new SimulatedAgvSpawnerTestFixture Build()
    {
        AddConsumers(typeof(GetGraphResponder));
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