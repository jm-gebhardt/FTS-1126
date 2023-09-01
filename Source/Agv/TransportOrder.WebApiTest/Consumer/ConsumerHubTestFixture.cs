using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

public class ConsumerHubTestFixture<TConsumer, THubContext> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
    where THubContext : class
{
    public readonly Mock<THubContext> MockHub = new();
    public readonly Mock<INodeStateRegistry> MockNodeStateRegistry = new();
    private readonly Mock<IActionGroupRetrievalService> _mockActionGroupRetrievalService = new();
        
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
            new global::TransportOrder.Messages.Profiles.AgvProfile(),
            new global::TransportOrder.Messages.Profiles.AgvGraphProfile(),
            new global::TransportOrder.Messages.Profiles.AgvErrorProfile(),
            new global::TransportOrder.Messages.Profiles.MapProfile()
        );
        return mapper;
    }

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => GetMapper());
        collection.AddTransient(_ => MockHub.Object);
        collection.AddTransient(_ => MockNodeStateRegistry.Object);
        collection.AddTransient(_ => _mockActionGroupRetrievalService.Object);
        collection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }
}