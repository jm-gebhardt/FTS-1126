using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

public class AgvCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvCommandHandler>
{
    public readonly Mock<IAgvRepository> AgvRepository = new();

    public AgvCommandHandlerTestFixture()
    {
        AgvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>())).Verifiable();
        AddServices(sc => sc.AddTransient(_ => AgvRepository.Object));
        Build();
    }

    public AgvCommandHandlerTestFixture AddAgv(Agv agv)
    {
        AgvRepository.Setup(x => x.GetAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }

    public AgvCommandHandlerTestFixture SetNoAgv(Guid correlationId)
    {
        AgvRepository.Setup(x => x.GetAgvAsync(correlationId)).ReturnsAsync((Agv?)null);
        return this;
    }
}