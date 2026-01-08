using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

public class CargoTriggerPointEventHandlerTestFixture : MassTransitConsumerTestFixture<CargoTriggerPointEventHandler>
{
    private readonly Mock<IHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{CargoTriggerPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<CargoTriggerPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<CargoTriggerPointEventHandler>>().Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddSingleton(_ => _stationRepository.Object);
        return this;
    }

    /// <inheritdoc cref="CargoTriggerPointEventHandlerTestFixture"/>>
    public CargoTriggerPointEventHandlerTestFixture()
    {
        Build();
    }

    public CargoTriggerPointEventHandlerTestFixture WithHostOrder(HostOrder? order)
    {
        if (order != null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        }
        return this;
    }

    public CargoTriggerPointEventHandlerTestFixture WithStation(Station? station)
    {
        _stationRepository.Setup(x => x.GetStationByName(It.IsAny<string>())).ReturnsAsync(station);
        return this;
    }
}
