using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.EventHandler;

public class GraphLayoutStoredHandlerTestFixture : MassTransitConsumerTestFixture<LayoutEventHandler>
{
    private readonly Mock<IAgvRepository> _agvRepository = new();

    public GraphLayoutStoredHandlerTestFixture()
    {
        AddServices(sc => sc.AddTransient(_ => _agvRepository.Object));
        Build();
    }

    public GraphLayoutStoredHandlerTestFixture SetAgvs(params Agv[] agvs)
    {
        _agvRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(agvs);
        return this;
    }
}