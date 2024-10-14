using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;

namespace Gebhardt.Acc.PrismatCommunicationTest.PrismatApi.TelegramApi.Processors;

public class FromPrismatTelegramProcessorTestFixture : TelegramProcessorTestFixture<FromPrismatTelegramProcessor, PrismatTelegram>
{
    private readonly Mock<TelegramProcessorFactory> _processorFactory;
    private readonly Mock<TelegramProcessorBase<PrismatTelegram>> _processorMock = new();

    public FromPrismatTelegramProcessorTestFixture()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var factoryProcessorLogger = new Mock<ILogger<TelegramProcessorFactory>>();

        _processorFactory = new Mock<TelegramProcessorFactory>(serviceProvider.Object, factoryProcessorLogger.Object);

        AddServices(s =>
        {
            s.AddKeyedSingleton(ServiceKeyProvider.FromPrismat, (_, _) => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => _processorFactory.Object);
        });
        _processorMock.Setup(s => s.ProcessReceivedTelegram(It.IsAny<string>())).Verifiable();
        _processorFactory.Setup(f => f.GetReceiveProcessor(It.IsAny<PrismatTelegram>())).Returns(_processorMock.Object);
    }

    public FromPrismatTelegramProcessorTestFixture WithoutCurrentProcessor()
    {
        _processorFactory.Setup(f => f.GetReceiveProcessor(It.IsAny<PrismatTelegram>())).Returns((ITelegramProcessor?)null);
        return this;
    }

    public FromPrismatTelegramProcessorTestFixture WithCurrentProcessor(string nextTelegram = "", bool withNextTelegram = false)
    {
        _processorMock.Setup(s => s.GetNextTelegramToSend(out nextTelegram)).Returns(withNextTelegram);
        _processorMock.Setup(s => s.Cancel()).Verifiable();
        var property = typeof(FromPrismatTelegramProcessor).GetProperty("CurrentProcessor");
        property!.SetValue(Processor, _processorMock.Object);
        return this;
    }

    public FromPrismatTelegramProcessorTestFixture WithCurrentTelegram(PrismatTelegram telegram)
    {
        var property = typeof(FromPrismatTelegramProcessor).GetProperty("CurrentTelegram");
        property!.SetValue(Processor, telegram);
        return this;
    }

    public new FromPrismatTelegramProcessorTestFixture Build()
    {
        base.Build();
        return this;
    }

    public void VerifyTelegramProcessed(string telegram, Times callTimes)
    {
        _processorMock.Verify(p => p.ProcessReceivedTelegram(It.Is<string>(s => s == telegram)), callTimes);
    }
}