using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class TelegramProcessorTestFixture<TProcessor, TTelegramType> : MassTransitTestFixture
    where TTelegramType : TelegramBase
    where TProcessor : TelegramProcessorBase<TTelegramType>
{
    public TProcessor Processor { get; private set; } = null!;

    public TelegramProcessorTestFixture(bool? useToPrismatConfiguration=false)
    {
        string serviceKey = useToPrismatConfiguration != null && useToPrismatConfiguration.Value ? ServiceKeyProvider.ToPrismat : ServiceKeyProvider.FromPrismat;
        AddServices(s =>
        {
            s.AddKeyedSingleton(serviceKey, (_, _) => TestConfigurationProvider.GetConfiguration());
            if (useToPrismatConfiguration == null)
            {
                s.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
            }
            s.AddSingleton(_ => new Mock<ILogger<TProcessor>>().Object);
            s.AddSingleton<TProcessor>();
            s.AddSingleton(_ => new ObservableConcurrentQueue<TTelegramType>());
        });
    }

    public new TelegramProcessorTestFixture<TProcessor, TTelegramType> Build()
    {
        base.Build();
        Processor = (TProcessor)Provider.GetRequiredService(typeof(TProcessor));
        return this;
    }
}