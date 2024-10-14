using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Fixtures;

public class TelegramHostCommunicatorTestFixture : FixtureBase
{
    public ToPrismatTelegramProcessor Processor { get; private set; } = null!;

    public TelegramHostCommunicator Communicator { get; private set; } = null!;

    public TelegramHostCommunicatorTestFixture()
    {
        AddServices(s =>
        {
            s.AddKeyedSingleton(ServiceKeyProvider.ToPrismat, (_, _) => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => new Mock<ILogger<ToPrismatTelegramProcessor>>().Object);
            s.AddSingleton(_ => new Mock<ILogger<TelegramHostCommunicator>>().Object);
            s.AddSingleton<ToPrismatTelegramProcessor>();
            s.AddSingleton<TelegramHostCommunicator>();
        });
    }

    public new TelegramHostCommunicatorTestFixture Build()
    {
        base.Build();
        Processor = Provider.GetRequiredService<ToPrismatTelegramProcessor>();
        Communicator = Provider.GetRequiredService<TelegramHostCommunicator>();
        return this;
    }
}