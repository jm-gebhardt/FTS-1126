using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

public class TelegramProcessorFactoryTestFixture : FixtureBase
{
    public readonly TelegramProcessorFactory Factory;

    public TelegramProcessorFactoryTestFixture()
    {
        var busMock = new Mock<IBus>();

        AddServices(s =>
        {
            s.AddSingleton(_ => new TordProcessor(busMock.Object, new Mock<ILogger<TordProcessor>>().Object, TestConfigurationProvider.GetConfiguration()));
            s.AddSingleton(_ => new LifeProcessor(new Mock<ILogger<LifeProcessor>>().Object, TestConfigurationProvider.GetConfiguration()));
            s.AddSingleton(_ => new Mock<ILogger<TelegramProcessorFactory>>().Object);
            s.AddSingleton<TelegramProcessorFactory>();
        });
        Build();
        Factory = Provider.GetRequiredService<TelegramProcessorFactory>();
    }
}