using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

public class StationOnlineStateCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationOnlineStateCommandHandler>
{
    public Mock<IPlcConnectionRepository> ConnectionRepositoryMock { get; } = new();

    public StationOnlineStateCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<StationOnlineStateCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddSingleton(ConnectionRepositoryMock.Object);
        return this;
    }
}