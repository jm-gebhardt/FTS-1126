using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class TelegramProcessorTestFixture<TProcessor, TTelegramType> : MassTransitTestFixture
    where TTelegramType : TelegramBase
    where TProcessor : TelegramProcessorBase<TTelegramType>
{
    public TProcessor Processor { get; private set; } = null!;

    public TelegramProcessorTestFixture()
    {
        AddServices(s =>
        {
            s.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => new Mock<ILogger<TProcessor>>().Object);
            s.AddSingleton<TProcessor>();
        });
    }

    public new TelegramProcessorTestFixture<TProcessor, TTelegramType> Build()
    {
        base.Build();
        Processor = (TProcessor)Provider.GetRequiredService(typeof(TProcessor));
        return this;
    }
}