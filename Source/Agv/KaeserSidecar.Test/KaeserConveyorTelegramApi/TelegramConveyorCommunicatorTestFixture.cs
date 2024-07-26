using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

public class TelegramConveyorCommunicatorTestFixture : FixtureBase
{
    public ToConveyorTelegramProcessor Processor { get; private set; } = null!;

    public TelegramConveyorCommunicator Communicator { get; private set; } = null!;

    public TelegramConveyorCommunicatorTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => new Mock<ILogger<TelegramConveyorCommunicator>>().Object);
            s.AddSingleton(_ => new Mock<ILogger<ToConveyorTelegramProcessor>>().Object);
            s.AddSingleton(_ => new FakeTimeProvider());
            s.AddSingleton<ToConveyorTelegramProcessor>();
            s.AddSingleton<TelegramConveyorCommunicator>();
        });
    }

    public new TelegramConveyorCommunicatorTestFixture Build()
    {
        base.Build();
        Processor = Provider.GetRequiredService<ToConveyorTelegramProcessor>();
        Communicator = Provider.GetRequiredService<TelegramConveyorCommunicator>();
        return this;
    }
}