using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.Services.EventHandler;

public class JamStateEventHandlerTestFixture: MassTransitConsumerTestFixture<JamStateEventHandler>
{
    private readonly Mock<IStationRepository> _stationRepository = new();

    protected override MassTransitConsumerTestFixture<JamStateEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _stationRepository.Object);
        return this;
    }

    public JamStateEventHandlerTestFixture()
    {
        Build();
    }
}