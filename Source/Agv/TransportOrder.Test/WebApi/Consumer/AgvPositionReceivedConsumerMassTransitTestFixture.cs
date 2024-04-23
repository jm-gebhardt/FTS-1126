using AutoMapper;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

internal class AgvPositionReceivedConsumerMassTransitTestFixture :
    MassTransitConsumerTestFixture<AgvPositionReceivedEventHandler>
{
    private readonly Mock<IGraphRepository> _graphRepositoryMock = new();

    public Mock<IAgvPositionRepository>? AgvPositionRepositoryMock { get; private set; }

    protected override IEnumerable<Profile> GetProfiles() =>
    [
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
        new TransportOrder.Domain.Profiles.AgvPositionProfile()
    ];

    public AgvPositionReceivedConsumerMassTransitTestFixture UseAgvPositionRepositoryMock()
    {
        AddServices(s =>
        {
            AgvPositionRepositoryMock = new Mock<IAgvPositionRepository>();

            s.AddSingleton(AgvPositionRepositoryMock.Object);
            s.AddSingleton(_graphRepositoryMock.Object);
        });

        return this;
    }

    public new AgvPositionReceivedConsumerMassTransitTestFixture Build()
    {
        base.Build();
        return this;
    }

    protected override MassTransitConsumerTestFixture<AgvPositionReceivedEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        if (AgvPositionRepositoryMock is null)
        {
            collection.AddSingleton<IAgvPositionRepository, AgvPositionRepository>();
            collection.AddSingleton(_graphRepositoryMock.Object);
        }

        collection.AddTransient(_ => new Mock<IAgvPublisher>().Object);
        collection.AddSingleton<ISignificanceChecker<AgvPosition>, AgvPositionSignificanceChecker>();
        collection.AddSingleton(new AgvPositionConfiguration(0.5, Mathematics.ConvertToRadians(5), 1000));

        return this;
    }
}