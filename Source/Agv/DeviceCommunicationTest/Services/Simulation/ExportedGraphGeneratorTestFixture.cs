using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public class ExportedGraphGeneratorTestFixture: FixtureBase
{
    private readonly Mock<IGraphReader> _graphReader = new();
    private readonly Mock<IMapGenerator> _mapGenerator = new();

    public readonly ExportedGraphGenerator Generator;

    public ExportedGraphGeneratorTestFixture(bool useRealData=false, KarisExportConfiguration? configuration=null)
    {
        AddServices(s =>
        {
            if (useRealData)
            {
                s.AddTransient<IGraphReader, JsonGraphReader>();
            }
            else
            {
                s.AddTransient(_ => _graphReader.Object);
            }
            
            s.AddTransient(_ => _mapGenerator.Object);
            s.AddTransient<ExportedGraphGenerator>();
            s.AddSingleton(_ => configuration?? new KarisExportConfiguration());
        });
        Build();
        Generator = Provider.GetRequiredService<ExportedGraphGenerator>();
    }

    public ExportedGraphGeneratorTestFixture WithExportedGraph(ExportedGraph graph)
    {
        _graphReader.Setup(x => x.Read()).Returns(graph);
        return this;
    }

    public ExportedGraph GetRealGraph()
    {
        var reader = Provider.GetRequiredService<IGraphReader>();
        return reader.Read();
    }
}