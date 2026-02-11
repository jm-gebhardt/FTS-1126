using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Map;

public class MapControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, MapController>
    where TRequest : class
    where TRequestHandler : class, IConsumer
{
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<INodeRepository> _nodeRepositoryMock = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new(); 

    public Mock<IMapRepository> MapRepositoryMock { get; private set; } = null!;

    protected override MapController GetController()
    {
        IRequestClient<GetBackgroundImage> getBackgroundImageRequestClient = GetRequestClient<GetBackgroundImage>();
        IRequestClient<SetBackground> setBackgroundRequestClient = GetRequestClient<SetBackground>();
        IRequestClient<DeleteBackground> deleteBackgroundRequestClient = GetRequestClient<DeleteBackground>();

        IRequestClient<GetMap> getMapRequestClient = GetRequestClient<GetMap>();
        IRequestClient<UpdateMap> updateMapRequestClient = GetRequestClient<UpdateMap>();
        IRequestClient<GetMapGraph> getMapGraphClient = GetRequestClient<GetMapGraph>();
        IRequestClient<GetMapNodeStates> getMapNodeStatesClient = GetRequestClient<GetMapNodeStates>();
        IRequestClient<GetMapStations> getStationsClient = GetRequestClient<GetMapStations>();
        IRequestClient<TrySettingNodeApproachingProperties> nodeApproachingPropertiesClient = GetRequestClient<TrySettingNodeApproachingProperties>();
        var loggingService = new LoggingService(_logEntryRepository.Object, TestHarness.Bus, Mapper, _actionGroupRepository.Object);
        var controller = new MapController(Mapper,
            new Mock<ILogger<MapController>>().Object,
            getBackgroundImageRequestClient,
            setBackgroundRequestClient,
            deleteBackgroundRequestClient,
            getMapRequestClient,
            updateMapRequestClient,
            getMapGraphClient,
            getMapNodeStatesClient,
            getStationsClient,
            nodeApproachingPropertiesClient,
            loggingService
            );
        return controller;
    }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new MapProfile(),
        new Gebhardt.Acc.TransportOrder.Messages.Profiles.MapProfile(),
        new LogEntryProfile()
    ];

    public MapControllerTestFixture(ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName)> nodeStates) : this()
    {
        _nodeRepositoryMock.Setup(x => x.GetNodeStatesForMapAsync(It.IsAny<Guid>())).ReturnsAsync(nodeStates);
    }

    public MapControllerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _nodeRepositoryMock.Object);
            s.AddTransient(_ => _graphRepository.Object);
            s.AddTransient(_ => _logEntryRepository.Object);
            s.AddTransient<ILoggingService, LoggingService>();
        });
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
    }

    /// <summary>
    /// Adds the <see cref="IMapRepository"/> mock object
    /// </summary>
    /// <returns></returns>
    public MapControllerTestFixture<TRequest, TRequestHandler> AddMapRepositoryMock(MapBackground? mapBackground)
    {
        MapRepositoryMock = new Mock<IMapRepository>();

        MapRepositoryMock.Setup(x => x.GetAllMapsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Map.Map>(new List<TransportOrder.Domain.Models.Map.Map>
        {
            MapGenerator.Generate(),
            MapGenerator.Generate(),
            MapGenerator.Generate()
        }));
        if (mapBackground != null)
        {
            MapRepositoryMock.Setup(x => x.GetMapAsync(mapBackground.CorrelationId)).ReturnsAsync(MapGenerator.Generate());
            MapRepositoryMock.Setup(x => x.GetBackgroundImageAsync(It.IsAny<Guid>())).ReturnsAsync(mapBackground.Image);
        }
        else
        {
            MapRepositoryMock.Setup(x => x.GetMapAsync(It.IsAny<Guid>())).ThrowsAsync(new MapNotFoundException("Map not found"));
            MapRepositoryMock.Setup(x => x.GetBackgroundImageAsync(It.IsAny<Guid>())).ThrowsAsync(new MapNotFoundException("Map not found"));
        }
        AddServices(sc => sc.AddTransient(_ => MapRepositoryMock.Object));

        return this;
    }

    public new MapControllerTestFixture<TRequest, TRequestHandler> Build()
    {
        base.Build();
        return this;
    }
}