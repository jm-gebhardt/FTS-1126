using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Domain.Services;
using Gebhardt.Acc.StationCommunication.Services.CommandHandler;
using Gebhardt.Acc.StationCommunication.SimulationMode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

public class JamStateCommandHandlerTestFixture : MassTransitConsumerTestFixture<JamStateCommandHandler>
{
    private readonly Mock<IJamStateRepository> _jamStateRepository = new();

    protected override MassTransitConsumerTestFixture<JamStateCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<JamStateCommandHandler>>().Object);
        collection.AddSingleton(_ => _jamStateRepository.Object);
        collection.AddSingleton<IConveyorCommunicator, SimulationConveyorCommunicator>();
        return this;
    }

    public JamStateCommandHandlerTestFixture()
    {
        Build();
        _jamStateRepository.Setup(x => x.AddAsync(It.IsAny<JamState>())).Verifiable();
        _jamStateRepository.Setup(x => x.UpdateAsync(It.IsAny<JamState>())).Verifiable();
    }

    public JamStateCommandHandlerTestFixture WithJamState(JamState? jamState)
    {
        _jamStateRepository.Setup(x => x.GetByStationAsync(It.IsAny<Guid>())).ReturnsAsync(jamState);
        return this;
    }

    public void VerifyJamStateAdded(Times times)
    {
        _jamStateRepository.Verify(x => x.AddAsync(It.IsAny<JamState>()), times);
    }

    public void VerifyJamStateUpdated(Times times)
    {
        _jamStateRepository.Verify(x => x.UpdateAsync(It.IsAny<JamState>()), times);
    }
}