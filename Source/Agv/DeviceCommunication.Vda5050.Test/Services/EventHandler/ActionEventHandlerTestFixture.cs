using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

public class ActionEventHandlerTestFixture: MassTransitConsumerTestFixture<ActionEventHandler>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();
   
    public ActionEventHandlerTestFixture()
    {
        _agvRepository.Setup(x => x.AddOrUpdateAsync(It.IsAny<Agv>())).Verifiable();
        AddServices(sc => sc.AddTransient(_ => _agvRepository.Object));
        Build();
    }

    public ActionEventHandlerTestFixture AddAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }

    public void VerifyAgvUpdated()
    {
        _agvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>()), Times.Once);
    }
}