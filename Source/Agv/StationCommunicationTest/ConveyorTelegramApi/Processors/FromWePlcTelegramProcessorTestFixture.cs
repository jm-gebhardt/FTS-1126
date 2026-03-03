using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.ConveyorTelegramApi.Processors;
using Gebhardt.Acc.ConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.StationCommunication.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi.Processors;

public class FromWePlcTelegramProcessorTestFixture : TelegramProcessorTestFixture<FromWePlcTelegramProcessor, ConveyorTelegram>
{
    private readonly Mock<TelegramProcessorFactory> _processorFactory;
    private readonly Mock<TelegramProcessorBase<ConveyorTelegram>> _processorMock = new();

    public FromWePlcTelegramProcessorTestFixture() : base(ServiceKeyProvider.FromWeStation)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var factoryProcessorLogger = new Mock<ILogger<TelegramProcessorFactory>>();

        _processorFactory = new Mock<TelegramProcessorFactory>(serviceProvider.Object, factoryProcessorLogger.Object);

        AddServices(s =>
        {
            s.AddKeyedSingleton(ServiceKeyProvider.FromWeStation, (_, _) => TestConfigurationProvider.GetConfiguration());
            s.AddSingleton(_ => _processorFactory.Object);
        });
        _processorMock.Setup(s => s.ProcessReceivedTelegram(It.IsAny<string>())).Verifiable();
        _processorFactory.Setup(f => f.GetReceiveProcessor(It.IsAny<ConveyorTelegram>())).Returns(_processorMock.Object);
    }

    public FromWePlcTelegramProcessorTestFixture WithoutCurrentProcessor()
    {
        _processorFactory.Setup(f => f.GetReceiveProcessor(It.IsAny<ConveyorTelegram>())).Returns((ITelegramProcessor?)null);
        return this;
    }

    public FromWePlcTelegramProcessorTestFixture WithCurrentProcessor(string nextTelegram = "", bool withNextTelegram = false)
    {
        _processorMock.Setup(s => s.GetNextTelegramToSend(out nextTelegram)).Returns(withNextTelegram);
        _processorMock.Setup(s => s.Cancel()).Verifiable();
        var property = typeof(FromWePlcTelegramProcessor).GetProperty("CurrentProcessor");
        property!.SetValue(Processor, _processorMock.Object);
        return this;
    }

    public FromWePlcTelegramProcessorTestFixture WithCurrentTelegram(ConveyorTelegram telegram)
    {
        var property = typeof(FromWePlcTelegramProcessor).GetProperty("CurrentTelegram");
        property!.SetValue(Processor, telegram);
        return this;
    }

    public new FromWePlcTelegramProcessorTestFixture Build()
    {
        base.Build();
        return this;
    }

    public void VerifyTelegramProcessed(string telegram, Times callTimes)
    {
        _processorMock.Verify(p => p.ProcessReceivedTelegram(It.Is<string>(s => s == telegram)), callTimes);
    }
}
