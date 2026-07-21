using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.RequestResponseHandler;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.RequestResponseHandler;

public class SimulatedAgvRequestHandlerTestFixture : MediatorTestFixture<RequestAgvState, SimulatedAgvRequestHandler, MockConsumer>
{
    private readonly Mock<IAgvCache> _agvCache = new();
    private readonly Mock<IAgvStateCache> _stateCache = new();

    public SimulatedAgvRequestHandlerTestFixture(bool agvExists = true, MqttState? cachedState = null)
    {
        _agvCache.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(agvExists);
        _stateCache.Setup(x => x.TryGetAsync(It.IsAny<Guid>())).ReturnsAsync(cachedState);
        AddServices(s =>
        {
            s.AddSingleton(_agvCache.Object);
            s.AddSingleton(_stateCache.Object);
        });
        base.Build();
    }

    public static SimulatedAgvRequestHandlerTestFixture WithUnknownAgv()
    {
        return new SimulatedAgvRequestHandlerTestFixture(false);
    }

    public static SimulatedAgvRequestHandlerTestFixture WithCachedState(MqttState cachedState)
    {
        return new SimulatedAgvRequestHandlerTestFixture(cachedState: cachedState);
    }
}