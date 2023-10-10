using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AgvPositionProfile = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Profiles.AgvPositionProfile;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
    where THubContext : class
{
    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalServiceMock = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistryMock = new();
    public readonly Mock<THubContext> PublisherMock = new();
    public readonly Mock<INodeStateRegistry> NodeStateRegistryMock = new();

    public Mock<IAgvRouteRetrievalService> AgvRouteServiceMock { get; } = new();

    public ConsumerHubTestFixture(params Action<IServiceCollection>[] services)
        : base(services: services)
    {
    }

    private IMapper GetMapper()
    {
        var mapper = MappingHelper.GetMapper(
            new AgvProfile(),
            new AgvGraphProfile(),
            new AgvPositionProfile(),
            new MapProfile(),
            new AgvErrorProfile(),
            new ActionProfile(),
            new Messages.Profiles.AgvProfile(),
            new Messages.Profiles.AgvGraphProfile(),
            new Messages.Profiles.AgvErrorProfile(),
            new Messages.Profiles.MapProfile(),
            new Messages.Profiles.AgvPositionProfile(),
            new ActionGroupProfile()
        );
        return mapper;
    }

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => PublisherMock.Object);
        collection.AddTransient(_ => NodeStateRegistryMock.Object);
        collection.AddTransient(_ => _actionGroupRetrievalServiceMock.Object);
        collection.AddTransient(_ => AgvRouteServiceMock.Object);
        collection.AddTransient(_ => _actionGroupRegistryMock.Object);
        collection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }
}