using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public sealed class ExportedGraphGeneratorTestFixture: FixtureBase
{
    private readonly Mock<IMapGenerator> _mapGenerator = new();

    public readonly ExportedGraphGenerator Generator;

    public ExportedGraphGeneratorTestFixture(KarisExportConfiguration? configuration=null)
    {
        AddServices(s =>
        {
            s.AddTransient<IGraphReader, JsonGraphReader>();
            s.AddTransient(_ => _mapGenerator.Object);
            s.AddTransient<ExportedGraphGenerator>();
            s.AddSingleton(_ => configuration?? new KarisExportConfiguration());
        });
        Build();
        Generator = Provider.GetRequiredService<ExportedGraphGenerator>();
    }

    public ExportedGraph GetRealGraph()
    {
        var reader = Provider.GetRequiredService<IGraphReader>();
        return reader.Read();
    }
}