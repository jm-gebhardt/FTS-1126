using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Services;
using Gebhardt.Acc.StationCommunication.Services.CommandHandler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>, IDisposable
{
    private string? _tempFilePath;

    private readonly StationConfiguration _stationConfiguration = new()
    {
        Stations = []
    };

    public Mock<IPlcConnectionRepository> RepositoryMock { get; } = new();

    protected override MassTransitConsumerTestFixture<StationCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(_ => _stationConfiguration);
        collection.AddSingleton<IPlcConnectionRepository>(_ => RepositoryMock.Object);
        collection.AddSingleton(new ActiveChannelKeyProvider(new ConfigurationBuilder().Build()));
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