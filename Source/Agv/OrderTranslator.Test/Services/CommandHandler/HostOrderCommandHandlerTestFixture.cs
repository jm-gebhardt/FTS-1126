using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using Gebhardt.Acc.OrderTranslator.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

public class HostOrderCommandHandlerTestFixture : MassTransitConsumerTestFixture<HostOrderCommandHandler>
{
    private readonly Mock<IHostOrderValidationService> _validationService = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IHostOrderRepository> _hostOrderRepository = new();

    protected override MassTransitConsumerTestFixture<HostOrderCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _validationService.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _hostOrderRepository.Object);
        _hostOrderRepository.Setup(x => x.Add(It.IsAny<HostOrder>())).Verifiable();
        _hostOrderRepository.Setup(x => x.SaveAsync()).Verifiable();
        return this;
    }

    public HostOrderCommandHandlerTestFixture()
    {
        Build();
    }

    public HostOrderCommandHandlerTestFixture WithStations(ICollection<Station> pickStations, ICollection<Station> dropStations)
    {
        _stationRepository.Setup(x => x.GetStationsByLane(It.IsAny<string>())).ReturnsAsync((string lane) =>
        {
            if (pickStations.SelectMany(s => s.InputLanes).Contains(lane))
            {
                return pickStations;
            }
            if (dropStations.SelectMany(s => s.OutputLanes).Contains(lane))
            {
                return dropStations;
            }
            return [];
        });
        return this;
    }

    public HostOrderCommandHandlerTestFixture WithValidationError(ValidationError? error)
    {
        _validationService.Setup(x => x.ValidateAsync(It.IsAny<HostOrder>())).ReturnsAsync(error);
        return this;
    }

    public void VerifyHostOrderAdded(Times times)
    {
        _hostOrderRepository.Verify(x => x.Add(It.IsAny<HostOrder>()), times);
        _hostOrderRepository.Verify(x => x.SaveAsync(), times);
    }
}