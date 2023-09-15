using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Persistence.Ef.Repositories;
using AgvErrorProfile = TransportOrder.Messages.Profiles.AgvErrorProfile;
using AgvGraphProfile = TransportOrder.Messages.Profiles.AgvGraphProfile;
using AgvPositionProfile = TransportOrder.Messages.Profiles.AgvPositionProfile;
using AgvProfile = TransportOrder.Messages.Profiles.AgvProfile;
using MapProfile = TransportOrder.Messages.Profiles.MapProfile;

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
            new WebApi.Profiles.AgvProfile(),
            new WebApi.Profiles.AgvGraphProfile(),
            new WebApi.Profiles.AgvPositionProfile(),
            new WebApi.Profiles.MapProfile(),
            new WebApi.Profiles.AgvErrorProfile(),
            new ActionProfile(),
            new AgvProfile(),
            new AgvGraphProfile(),
            new AgvErrorProfile(),
            new MapProfile(),
            new AgvPositionProfile()
        );
        return mapper;
    }
}