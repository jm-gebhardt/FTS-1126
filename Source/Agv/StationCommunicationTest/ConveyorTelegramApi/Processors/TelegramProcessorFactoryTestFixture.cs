using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

public sealed class TelegramProcessorFactoryTestFixture : FixtureBase
{
    public readonly TelegramProcessorFactory Factory;

    public TelegramProcessorFactoryTestFixture()
    {
        var busMock = new Mock<IBus>();

        AddServices(s =>
        {
            s.AddSingleton(_ => new JamStateProcessor(new Mock<ILogger<JamStateProcessor>>().Object, TestConfigurationProvider.GetConfiguration(), busMock.Object));
            s.AddSingleton(_ => new LifeProcessor(new Mock<ILogger<LifeProcessor>>().Object, TestConfigurationProvider.GetConfiguration()));
            s.AddSingleton(_ => new Mock<ILogger<TelegramProcessorFactory>>().Object);
            s.AddSingleton<TelegramProcessorFactory>();
        });
        Build();
        Factory = Provider.GetRequiredService<TelegramProcessorFactory>();
    }
}