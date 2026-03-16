using AutoMapper;
using AutoMapper.Internal;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.ActionProfile;
using AgvPositionProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.AgvPositionProfile;
using LogEntryProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.LogEntryProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
    where THubContext : class
{
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    public readonly Mock<THubContext> PublisherMock = new();
    protected readonly Mock<INodeRepository> NodeRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new AgvProfile(),
        new AgvGraphProfile(),
        new AgvPositionProfile(),
        new MapProfile(),
        new AgvErrorProfile(),
        new TransportOrder.WebApi.Profiles.ActionProfile(),
        new TransportOrder.Messages.Profiles.AgvProfile(),
        new TransportOrder.Messages.Profiles.AgvGraphProfile(),
        new TransportOrder.Messages.Profiles.AgvErrorProfile(),
        new TransportOrder.Messages.Profiles.MapProfile(),
        new TransportOrder.Messages.Profiles.AgvPositionProfile(),
        new ActionProfile(),
        new RestrictedAreaProfile(),
        new PolygonPointProfile(),
        new LogEntryProfile()
    ];

    public ConsumerHubTestFixture(params Action<IServiceCollection>[] services)
    {
        AddServices(services);
        Build();
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
    }

    public Mock<IAgvRouteRetrievalService> AgvRouteServiceMock { get; } = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => PublisherMock.Object);
        collection.AddTransient(_ => NodeRepositoryMock.Object);
        collection.AddTransient(_ => _actionGroupRetrievalServiceMock.Object);
        collection.AddTransient(_ => AgvRouteServiceMock.Object);
        collection.AddTransient(_ => _actionGroupRepositoryMock.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _logEntryRepository.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddAutoMapper(cfg => cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(10)), AppDomain.CurrentDomain.GetAssemblies());
        return this;
    }
}