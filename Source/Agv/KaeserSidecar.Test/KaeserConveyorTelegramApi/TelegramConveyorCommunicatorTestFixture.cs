using Gebhardt.Acc.Common.Domain.Utilities;
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

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

public class TelegramConveyorCommunicatorTestFixture: FixtureBase
{
    public ToConveyorTelegramProcessor Processor { get; private set; } = null!;

    public TelegramConveyorCommunicator Communicator { get; private set; } = null!;

    private readonly ObservableConcurrentQueue<ConveyorTelegram> _agvCargoStateQueue = new();
    private readonly ObservableConcurrentQueue<ConveyorTelegram> _aplWeStationQueue = new();

    public TelegramConveyorCommunicatorTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => new Mock<ILogger<TelegramConveyorCommunicator>>().Object);
            s.AddSingleton(_ => new Mock<ILogger<ToConveyorTelegramProcessor>>().Object);
            s.AddKeyedSingleton<ToConveyorTelegramProcessor>(ServiceKeyProvider.AplWeStation,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ToConveyorTelegramProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                    return new ToConveyorTelegramProcessor(logger, timeProvider, TestConfigurationProvider.GetConfiguration(), _aplWeStationQueue);
                });

            s.AddKeyedSingleton<ToConveyorTelegramProcessor>(ServiceKeyProvider.AgvCargoState,
                (serviceProvider, _) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ToConveyorTelegramProcessor>>();
                    var timeProvider = serviceProvider.GetRequiredService<ITimeProvider>();
                    return new ToConveyorTelegramProcessor(logger, timeProvider, TestConfigurationProvider.GetConfiguration(), _agvCargoStateQueue);
                });
            s.AddSingleton(_ => new FakeTimeProvider());
            s.AddSingleton<ToConveyorTelegramProcessor>();
            s.AddSingleton<TelegramConveyorCommunicator>();
        });
    }

    public TelegramConveyorCommunicatorTestFixture Build(string serviceKey)
    {
        base.Build();
        Processor = Provider.GetKeyedService<ToConveyorTelegramProcessor>(serviceKey)!;
        Communicator = Provider.GetRequiredService<TelegramConveyorCommunicator>();
        return this;
    }
}