using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class ConveyorReportingEventHandlerTestFixture : MassTransitConsumerTestFixture<ConveyorReportingEventHandler>
{
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IConveyorCommunicator> _communicator = new();
    private readonly Mock<IAgvSettingsService> _agvSettingsService = new();
    public readonly IOrderRepository OrderRepository = new OrderRepository();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{ConveyorReportingEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<ConveyorReportingEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<ConveyorReportingEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _communicator.Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddTransient(_ => _agvSettingsService.Object);
        collection.AddSingleton(_ => OrderRepository);
        return this;
    }

    /// <inheritdoc cref="ConveyorReportingEventHandlerTestFixture"/>>
    public ConveyorReportingEventHandlerTestFixture()
    {
        _communicator.Setup(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>())).Verifiable();

        Build();
    }

    public ConveyorReportingEventHandlerTestFixture WithHostOrders(HostOrder order, HostOrder? nextPickOrder=null)
    {
        if (nextPickOrder == null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
            return this;
        }
        _hostOrderRepository.SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order)
            .ReturnsAsync(nextPickOrder);
        return this;
    }

    public ConveyorReportingEventHandlerTestFixture WithAgvHasLowBatteryLevel(bool hasLowBatteryLevel)
    {
        _agvSettingsService.Setup(x => x.CheckAgvHasCriticalBatteryLevel(It.IsAny<Guid>())).ReturnsAsync(hasLowBatteryLevel);
        return this;
    }

    public void VerifyReportingToConveyors(string station = "none", string? dropStation = null, string? pickStation = null, string? hu = null, Times? times = null)
    {
        times ??= Times.Once();
        if (times == Times.Never())
        {
            _communicator.Verify(x => x.ReportPalletPickDropStation(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), (Times)times);
        }
        _communicator.Verify(x => x.ReportPalletPickDropStation(station, pickStation, dropStation, hu), (Times)times);
    }
}