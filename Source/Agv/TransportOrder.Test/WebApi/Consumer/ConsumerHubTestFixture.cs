using AutoMapper;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.ActionProfile;
using AgvPositionProfile = Gebhardt.Acc.TransportOrder.Domain.Profiles.AgvPositionProfile;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
    where THubContext : class
{
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    public readonly Mock<THubContext> PublisherMock = new();
    protected readonly Mock<INodeStateRepository> NodeStateRepositoryMock = new();
    private readonly Mock<IStationRepository> _stationRepository = new();

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
        new PolygonPointProfile()
    ];

    public ConsumerHubTestFixture(params Action<IServiceCollection>[] services)
    {
        AddServices(services);
        Build();
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
        _stationRepository.Setup(x => x.GetRequiredStationBaseInformation(It.IsAny<string>())).ReturnsAsync(new TransportOrder.Domain.Models.Station.Station());
    }

    public Mock<IAgvRouteRetrievalService> AgvRouteServiceMock { get; } = new();

    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => PublisherMock.Object);
        collection.AddTransient(_ => NodeStateRepositoryMock.Object);
        collection.AddTransient(_ => _actionGroupRetrievalServiceMock.Object);
        collection.AddTransient(_ => AgvRouteServiceMock.Object);
        collection.AddTransient(_ => _actionGroupRepositoryMock.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
        return this;
    }
}