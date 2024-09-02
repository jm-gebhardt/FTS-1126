using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

public class StationControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, StationController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    [UsedImplicitly]
    private readonly Mock<IStationRepository> _stationRepository = new();

    [UsedImplicitly]
    private readonly Mock<IMapRepository> _mapRepository = new();

    protected override StationController GetController()
    {
        IRequestClient<GetStation> getStationRequestClient = GetRequestClient<GetStation>();
        var controller = new StationController(Mapper, getStationRequestClient);
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new StationProfile()
    ];

    public new StationControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _stationRepository.Object);
        });

        base.Build();
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStations(It.IsAny<string?>())).ReturnsAsync([station]);
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithMap(TransportOrder.Domain.Models.Map.Map map)
    {
        _mapRepository.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync(map);
        return this;
    }
}