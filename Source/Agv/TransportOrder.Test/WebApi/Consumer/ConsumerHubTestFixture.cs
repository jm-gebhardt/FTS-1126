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
    public readonly Mock<INodeStateRepository> NodeStateRepositoryMock = new();

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