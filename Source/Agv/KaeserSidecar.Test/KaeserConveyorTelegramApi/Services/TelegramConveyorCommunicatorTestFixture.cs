using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi.Services;

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
                    var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                    return new ToWePlcTelegramProcessor(logger, timeProvider, TestConfigurationProvider.GetConfiguration(), _queue);
                });

            s.AddKeyedSingleton(ServiceKeyProvider.AgvCargoState,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<AgvAplCargoStateProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
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