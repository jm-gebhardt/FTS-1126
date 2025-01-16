using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.TelegramCommunication;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

public class CargoTriggerPointEventHandlerTestFixture : MassTransitConsumerTestFixture<CargoTriggerPointEventHandler>
{
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<IConveyorCommunicator> _communicator = new();
    private readonly Mock<ITransportOrderActionGroupRepository> _actionGroupRepository = new();

    /// <inheritdoc cref="MassTransitConsumerTestFixture{CargoTriggerPointEventHandler}"/>>
    protected override MassTransitConsumerTestFixture<CargoTriggerPointEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<CargoTriggerPointEventHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddSingleton(_ => TestConfigurationProvider.GetConfiguration());
        collection.AddSingleton(_ => _communicator.Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        return this;
    }

    /// <inheritdoc cref="CargoTriggerPointEventHandlerTestFixture"/>>
    public CargoTriggerPointEventHandlerTestFixture()
    {
        _communicator.Setup(x => x.ReportAplCargoState(It.IsAny<bool>())).Verifiable();

        Build();
    }

    public CargoTriggerPointEventHandlerTestFixture WithHostOrder(HostOrder? order)
    {
        if (order != null)
        {
            _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        }
        return this;
    }

    public CargoTriggerPointEventHandlerTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }

    public void VerifyReportingToConveyors(bool hasCargo = false, Times? times = null)
    {
        times ??= Times.Once();
        if (times == Times.Never())
        {
            _communicator.Verify(x => x.ReportAplCargoState(It.IsAny<bool>()), (Times)times);
        }
        _communicator.Verify(x => x.ReportAplCargoState(hasCargo), (Times)times);
    }
}
