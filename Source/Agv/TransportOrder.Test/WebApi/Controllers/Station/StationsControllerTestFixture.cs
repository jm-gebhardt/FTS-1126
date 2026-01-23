using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

public class StationsControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, StationsController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    [UsedImplicitly]
    private readonly Mock<IStationRepository> _stationRepository = new();

    [UsedImplicitly]
    private readonly Mock<IMapRepository> _mapRepository = new();

    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    protected override StationsController GetController()
    {
        IRequestClient<GetStations> getStationRequestClient = GetRequestClient<GetStations>();
        var controller = new StationsController(Mapper, getStationRequestClient);
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new StationProfile()
    ];

    public new StationsControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _stationRepository.Object);
            sc.AddTransient(_ => _actionGroupRepository.Object);
            sc.AddTransient(_ => _logEntryRepository.Object);
            sc.AddTransient<ILoggingService, LoggingService>();
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        base.Build();
        return this;
    }

    public StationsControllerTestFixture<TRequest, TRequestHandler> WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStations(It.IsAny<Guid?>())).ReturnsAsync([station]);
        return this;
    }

    public StationsControllerTestFixture<TRequest, TRequestHandler> WithMap(TransportOrder.Domain.Models.Map.Map map)
    {
        _mapRepository.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ReturnsAsync(map);
        return this;
    }
}