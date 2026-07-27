using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Station;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

public class StationControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, StationController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    [UsedImplicitly]
    private readonly Mock<IStationRepository> _stationRepository = new();

    [UsedImplicitly]
    private readonly Mock<INodeRepository> _nodeRepository = new();
    
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    private readonly Mock<IAgvDb> _agvDb = new();

    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    [UsedImplicitly]
    private readonly Mock<IMapRepository> _mapRepository = new();

    protected override StationController GetController()
    {
        IRequestClient<GetStation> getStationRequestClient = GetRequestClient<GetStation>();
        IRequestClient<TryLockStation> lockStationRequestClient = GetRequestClient<TryLockStation>();
        IRequestClient<TryUnlockStation> unlockStationRequestClient = GetRequestClient<TryUnlockStation>();
        IRequestClient<UnblockStation> unblockStationRequestClient = GetRequestClient<UnblockStation>();
        IRequestClient<SendJamStateRequest> sendJamStateRequestClient = GetRequestClient<SendJamStateRequest>();
        var controller = new StationController(Mapper, getStationRequestClient, lockStationRequestClient, unlockStationRequestClient,
            unblockStationRequestClient, sendJamStateRequestClient);
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new StationProfile(),
        new LogEntryProfile()
    ];

    public new StationControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsForStation(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        AddServices(sc =>
        {
            sc.AddTransient(_ => _mapRepository.Object);
            sc.AddTransient(_ => _stationRepository.Object);
            sc.AddTransient(_ => _nodeRepository.Object);
            sc.AddTransient(_ => _actionGroupRepository.Object);
            sc.AddTransient(_ => _agvDb.Object);
            sc.AddTransient(_ => _logEntryRepository.Object);
            sc.AddTransient<ILoggingService, LoggingService>();
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _, CancellationToken _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        base.Build();
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithUser(string? role, string? email)
    {
        var httpContext = new Mock<HttpContext>();

        if (role != null)
        {
            httpContext.Setup(
                x => x.User.IsInRole(It.Is<string>(s => s.Equals(role)))
            ).Returns(true);
        }
        if (email != null)
        {
            httpContext.Setup(x => x.User.FindFirst(ClaimTypes.Email)).Returns(new Claim(ClaimTypes.Email, email));
        }

        HttpContextOverride = httpContext.Object;

        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithStation(TransportOrder.Domain.Models.Station.Station station)
    {
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStations(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync([station]);
        _nodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GraphNode {
            Id = station.Id,
            Name = station.Name
        });
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithMap(TransportOrder.Domain.Models.Map.Map map)
    {
        _mapRepository.Setup(x => x.GetMapAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(map);
        return this;
    }

    public StationControllerTestFixture<TRequest, TRequestHandler> WithActionGroups(List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentActionGroupsForStation(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(actionGroups);
        _actionGroupRepository.Setup(x => x.GetPausedActionGroupsForStation(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(actionGroups.Where(g => g.State == ActionGroupState.Paused).ToList());
        return this;
    }
}