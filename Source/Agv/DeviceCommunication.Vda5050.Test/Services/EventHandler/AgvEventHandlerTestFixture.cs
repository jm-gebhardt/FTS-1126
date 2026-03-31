using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

public class AgvEventHandlerTestFixture : MassTransitConsumerTestFixture<AgvEventHandler>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public AgvEventHandlerTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => _agvRepository.Object));
        Build();
    }

    public AgvEventHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }
}