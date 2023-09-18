using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

public class AgvCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvCommandHandler>
{
    private readonly Mock<IChargingService> _chargingService = new();
    public readonly Mock<IAgvSagaCommunicationService> AgvSagaCommunicationService = new();
    private readonly ActionManagerSemaphoreProvider _semaphoreProvider = new();
    private readonly Mock<IActionGroupRegistry> _actionGroupRegistry = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => AgvSagaCommunicationService.Object);
        collection.AddTransient(_ => _chargingService.Object);
        collection.AddTransient<IActionManagerSemaphoreProvider>(_ => _semaphoreProvider);
        collection.AddTransient(_ => _actionGroupRegistry.Object);
    }

    public AgvCommandHandlerTestFixture()
    {
        Build();
    }

    public void AddChargingAgv(List<IAgv> agvs)
    {
        AgvSagaCommunicationService.Setup(x =>
            x.SetIdleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Verifiable();

        _chargingService.Setup(x =>
            x.GetChargingAgvsWithGoodBatteryLevel()).ReturnsAsync(agvs);
    }

    public void SetupAgv(bool hasPendingActions)
    {
        _actionGroupRegistry.Setup(x =>
            x.AgvHasPendingActions(It.IsAny<Guid>())).ReturnsAsync(hasPendingActions);
    }
}