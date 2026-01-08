using System.Collections.Concurrent;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Services;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Services;

public class TelegramConveyorCommunicatorTestFixture: FixtureBase
{
    public AgvAplCargoStateProcessor AplCargoStateProcessor { get; private set; } = null!;

    public ToWePlcTelegramProcessor ToWePlcTelegramProcessor { get; private set; } = null!;
   
    public TelegramConveyorCommunicator Communicator { get; private set; } = null!;

    private readonly ConcurrentQueue<ConveyorTelegram> _queue = new();

    private TelegramCommunicationConfiguration _configuration = null!;

    public TelegramConveyorCommunicatorTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton(_ => new Mock<ILogger<TelegramConveyorCommunicator>>().Object);
            s.AddSingleton(_ => new Mock<ILogger<ToWePlcTelegramProcessor>>().Object);
            s.AddSingleton(_ => new Mock<ILogger<AgvAplCargoStateProcessor>>().Object);
            s.AddKeyedSingleton(ServiceKeyProvider.FromWeStation,TestConfigurationProvider.GetConfiguration());
            s.AddKeyedSingleton(ServiceKeyProvider.AplWeStation,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ToWePlcTelegramProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                    return new ToWePlcTelegramProcessor(logger, timeProvider, TestConfigurationProvider.GetConfiguration(), _queue);
                });

            s.AddKeyedSingleton(ServiceKeyProvider.AgvCargoState,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<AgvAplCargoStateProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                    return new AgvAplCargoStateProcessor(logger, timeProvider, TestConfigurationProvider.GetConfiguration());
                });
            s.AddSingleton(_ => new FakeTimeProvider());
            s.AddSingleton<ToWePlcTelegramProcessor>();
            s.AddSingleton<AgvAplCargoStateProcessor>();
            s.AddSingleton<TelegramConveyorCommunicator>();
        });
    }

    public TelegramConveyorCommunicatorTestFixture Build(string serviceKey)
    {
        Build();
        ToWePlcTelegramProcessor = Provider.GetKeyedService<ToWePlcTelegramProcessor>(serviceKey)!;
        AplCargoStateProcessor = Provider.GetKeyedService<AgvAplCargoStateProcessor>(serviceKey)!;
        Communicator = Provider.GetRequiredService<TelegramConveyorCommunicator>();
        _configuration = Provider.GetKeyedService<TelegramCommunicationConfiguration>(ServiceKeyProvider.FromWeStation)!;
        return this;
    }

    public TelegramConveyorCommunicatorTestFixture DisableFromWePlcConnection()
    {
        _configuration.UseConnection = false;
        return this;
    }
}