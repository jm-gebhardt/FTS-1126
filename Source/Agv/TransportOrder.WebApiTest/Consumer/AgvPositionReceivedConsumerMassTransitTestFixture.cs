using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

internal class AgvPositionReceivedConsumerMassTransitTestFixture :
    MassTransitConsumerTestFixture<AgvPositionReceivedEventHandler>, ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture>
{
    private bool _useAgvPositionRegistryMock;
    public Mock<IAgvPositionRegistry>? AgvPositionRegistryMock { get; private set; }
    private Mock<IAgvGraphRegistry>? GraphRegistryMock { get; set; }

    public AgvPositionReceivedConsumerMassTransitTestFixture()
    {
        base.Build();
    }

    public ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture> UseAgvPositionRegistryMock()
    {
        _useAgvPositionRegistryMock = true;
        return this;
    }

    public AgvPositionReceivedConsumerMassTransitTestFixture Build()
    {
        base.Build();
        return this;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        GraphRegistryMock = new Mock<IAgvGraphRegistry>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        if (_useAgvPositionRegistryMock)
        {
            AgvPositionRegistryMock = new Mock<IAgvPositionRegistry>();

            services.AddSingleton(AgvPositionRegistryMock.Object);
            services.AddSingleton(GraphRegistryMock.Object);
        }
        else
        {
            services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
            services.AddSingleton(GraphRegistryMock.Object);
        }

        services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
        services.AddTransient(_ => new Mock<IAgvPublisher>().Object);
        services.AddTransient(_ => GetMapper());

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
            new Messages.Profiles.AgvPositionProfile()
        );
        return mapper;
    }
}