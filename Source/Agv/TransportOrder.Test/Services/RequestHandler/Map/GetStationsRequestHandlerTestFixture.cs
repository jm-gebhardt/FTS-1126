using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

public class GetStationsRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IMapRepository> _mapRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

    public GetStationsRequestHandlerTestFixture<TRequestClient, TConsumer> WithStation(Station station)
    {
        _stationRepository.Setup(x => x.GetStations(It.IsAny<string?>())).ReturnsAsync([station]);
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(station);
        return this;
    }

    public GetStationsRequestHandlerTestFixture<TRequestClient, TConsumer> WithMap(TransportOrder.Domain.Models.Map.Map map)
    {
        _mapRepository.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync(map);
        return this;
    }

    public override GetStationsRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _stationRepository.Object);
        });
        base.Build();
        return this;
    }
}