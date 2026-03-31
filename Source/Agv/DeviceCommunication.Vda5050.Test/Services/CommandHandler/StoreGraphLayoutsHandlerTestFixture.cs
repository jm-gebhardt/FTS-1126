using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class StoreGraphLayoutsHandlerTestFixture : MassTransitConsumerTestFixture<StoreGraphLayoutsHandler>
{
    public readonly Mock<IGraphLayoutRepository> GraphLayoutRepository = new();

    public StoreGraphLayoutsHandlerTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => GraphLayoutRepository.Object));
        Build();
    }
}