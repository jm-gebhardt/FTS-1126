using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionProfile = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles.ActionProfile;
using AgvPositionProfile = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles.AgvPositionProfile;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
    where THubContext : class
{
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepositoryMock = new();
    public readonly Mock<THubContext> PublisherMock = new();
    public readonly Mock<INodeStateRepository> NodeStateRepositoryMock = new();

    protected override IEnumerable<Profile> GetProfiles() =>
    [
        new AgvProfile(),
        new AgvGraphProfile(),
        new AgvPositionProfile(),
        new MapProfile(),
        new AgvErrorProfile(),
        new WebApi.Profiles.ActionProfile(),
        new Messages.Profiles.AgvProfile(),
        new Messages.Profiles.AgvGraphProfile(),
        new Messages.Profiles.AgvErrorProfile(),
        new Messages.Profiles.MapProfile(),
        new Messages.Profiles.AgvPositionProfile(),
        new ActionProfile()
    ];

    public ConsumerHubTestFixture(params Action<IServiceCollection>[] services)
    {
        AddServices(services);
        Build();
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
        collection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        return this;
    }
}