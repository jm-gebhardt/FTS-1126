using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Services.CommandHandler;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Gebhardt.Acc.OrderTranslator.Test.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

public class StationCommandHandlerTestFixture: MassTransitConsumerTestFixture<StationCommandHandler>, IDisposable
{
    private string? _tempFilePath;
    public readonly IStationRepository StationRepository = new StationRepositoryTestFixture().Repository;

    private readonly StationConfiguration _stationConfiguration = new()
    {
        Stations = [OrderGenerator.Apl1]
    };

    protected override FixtureBase AddServices(IServiceCollection collection)
    {   base.AddServices(collection);
        collection.AddTransient(_ => StationRepository);
        collection.AddSingleton(_ => _stationConfiguration);
        return this;
    }

    public StationCommandHandlerTestFixture()
    {
        Build();
    }

    public StationCommandHandlerTestFixture WithConfigurationFile(string yamlContent)
    {
        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, yamlContent);
        _stationConfiguration.ConfigFilePath = _tempFilePath;
        return this;
    }

    public void Dispose()
    {
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
}