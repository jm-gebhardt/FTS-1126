using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Common.Services.Significance;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

internal class AgvPositionReceivedConsumerMassTransitTestFixture :
    MassTransitConsumerTestFixture<AgvPositionReceivedEventHandler>, ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture>
{
    private bool _useAgvPositionRepositoryMock;

    public Mock<IAgvPositionRepository>? AgvPositionRepositoryMock { get; private set; }

    private Mock<IGraphRepository>? GraphRepositoryMock { get; set; }

    public AgvPositionReceivedConsumerMassTransitTestFixture()
    {
        base.Build();
    }

    public ITestFixtureBuilder<AgvPositionReceivedConsumerMassTransitTestFixture> UseAgvPositionRepositoryMock()
    {
        _useAgvPositionRepositoryMock = true;
        return this;
    }

    public AgvPositionReceivedConsumerMassTransitTestFixture Build()
    {
        base.Build();
        return this;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        GraphRepositoryMock = new Mock<IGraphRepository>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        if (_useAgvPositionRepositoryMock)
        {
            AgvPositionRepositoryMock = new Mock<IAgvPositionRepository>();

            services.AddSingleton(AgvPositionRepositoryMock.Object);
            services.AddSingleton(GraphRepositoryMock.Object);
        }
        else
        {
            services.AddSingleton<IAgvPositionRepository, AgvPositionRepository>();
            services.AddSingleton(GraphRepositoryMock.Object);
        }

        services.AddTransient(_ => new Mock<IAgvPublisher>().Object);
        services.AddSingleton<ISignificanceChecker<AgvPosition>, AgvPositionSignificanceChecker>();
        services.AddSingleton(new AgvPositionConfiguration(0.5, Mathematics.ConvertToRadians(5), 1000));
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
            new Domain.Profiles.AgvPositionProfile()
        );
        return mapper;
    }
}