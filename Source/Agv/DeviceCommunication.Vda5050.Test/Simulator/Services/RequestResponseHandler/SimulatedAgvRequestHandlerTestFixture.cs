using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
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
    public const string LoadId = "Load1";
    public const string LoadType = "pallet";
    public const string LoadPosition = "front";

    private readonly Mock<IAgvOperatingModeCache> _operatingModeCache = new();
    private readonly Mock<IAgvStateCache> _stateCache = new();

    public SimulatedAgvRequestHandlerTestFixture()
    {
        _operatingModeCache.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _operatingModeCache.Setup(x => x.GetOperatingModeAsync(It.IsAny<Guid>())).ReturnsAsync(AgvOperatingMode.Manual);
        _stateCache.Setup(x => x.TryGetAsync(It.IsAny<Guid>())).ReturnsAsync((MqttState?)null);
    }

    public SimulatedAgvRequestHandlerTestFixture WithUnknownAgv()
    {
        _operatingModeCache.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        _operatingModeCache.Setup(x => x.GetOperatingModeAsync(It.IsAny<Guid>())).ThrowsAsync(new AgvNotFoundException(Guid.Empty));
        return this;
    }

    public SimulatedAgvRequestHandlerTestFixture WithCachedState(MqttState cachedState)
    {
        _stateCache.Setup(x => x.TryGetAsync(It.IsAny<Guid>())).ReturnsAsync(cachedState);
        return this;
    }

    public SimulatedAgvRequestHandlerTestFixture WithOperatingMode(AgvOperatingMode operatingMode)
    {
        _operatingModeCache.Setup(x => x.GetOperatingModeAsync(It.IsAny<Guid>())).ReturnsAsync(operatingMode);
        return this;
    }

    public new SimulatedAgvRequestHandlerTestFixture Build()
    {
        AddServices(s =>
        {
            s.AddSingleton(_operatingModeCache.Object);
            s.AddSingleton(_stateCache.Object);
        });
        base.Build();
        return this;
    }
}