using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

public class ConveyorReportingEventHandlerTestFixture : MassTransitConsumerTestFixture<ConveyorReportingEventHandler>
{
    private readonly Mock<IHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{ConveyorReportingEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<ConveyorReportingEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<ConveyorReportingEventHandler>>().Object);
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddSingleton(_ => _stationRepository.Object);
        return this;
    }

    /// <inheritdoc cref="ConveyorReportingEventHandlerTestFixture"/>>
    public ConveyorReportingEventHandlerTestFixture()
    {
        Build();
    }

    public ConveyorReportingEventHandlerTestFixture WithHostOrders(HostOrder order, HostOrder? nextOrder = null)
    {
        if (nextOrder == null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
            return this;
        }
        _hostOrderRepository.SetupSequence(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(order)
            .ReturnsAsync(nextOrder);
        return this;
    }

    public ConveyorReportingEventHandlerTestFixture WithStation(Station? station)
    {
        _stationRepository.Setup(x => x.GetStationByName(It.IsAny<string>())).ReturnsAsync(station);
        return this;
    }
}