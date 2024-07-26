using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class ShortCircleTriggerPointEventHandlerTestFixture : MassTransitConsumerTestFixture<ShortCircleTriggerPointEventHandler>
{
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IConveyorCommunicator> _communicator = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{ShortCircleTriggerPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<ShortCircleTriggerPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<ShortCircleTriggerPointEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _communicator.Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        return this;
    }

    /// <inheritdoc cref="ShortCircleTriggerPointEventHandlerTestFixture"/>>
    public ShortCircleTriggerPointEventHandlerTestFixture()
    {
        _communicator.Setup(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>())).Verifiable();

        Build();
    }

    public ShortCircleTriggerPointEventHandlerTestFixture WithHostOrder(HostOrder? order)
    {
        if (order != null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        }
        _hostOrderRepository.Setup(x => x.GetNextPickOnShortCircle()).ReturnsAsync(order);
        return this;
    }

    public void VerifyReportingToConveyors(string station = "none", bool hasCargo = false, string? pickStation = null, Times? times = null)
    {
        times ??= Times.Once();
        if (times == Times.Never())
        {
            _communicator.Verify(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()), (Times)times);
        }
        _communicator.Verify(x => x.ReportPalletPickDropStation(station, hasCargo, pickStation), (Times)times);
    }
}