using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

public class AgvActionCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvActionCommandHandler>
{
    private readonly Mock<IPrismatCommunicationHostOrderRepository> _hostOrderRepository = new();
    private readonly Mock<ITransportOrderDb> _transportOrderDb = new();
    private readonly Mock<IConveyorCommunicator> _communicator = new();

    protected override MassTransitConsumerTestFixture<AgvActionCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => new Mock<ILogger<AgvActionCommandHandler>>().Object);
        collection.AddSingleton(_ => OrderGenerator.KaeserConfiguration);
        collection.AddTransient(_ => _transportOrderDb.Object);
        collection.AddSingleton(_ => _hostOrderRepository.Object);
        collection.AddSingleton(_ => _communicator.Object);
        return this;
    }

    public AgvActionCommandHandlerTestFixture()
    {
        _communicator.Setup(x => x.ReportPalletDrop(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        Build();
    }

    public AgvActionCommandHandlerTestFixture WithHostOrder(HostOrder order)
    {
        _hostOrderRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(order);
        return this;
    }

    public AgvActionCommandHandlerTestFixture WithActionGroup(ActionGroup? actionGroup)
    {
        _transportOrderDb.Setup(x => x.ActionGroups.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid _) =>
            {
                if (actionGroup != null)
                {
                    return actionGroup;
                }
                throw new Exception();
            });
        _transportOrderDb.Setup(x => x.ActionGroups.GetActionGroupAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid _) =>
            {
                if (actionGroup != null)
                {
                    return actionGroup;
                }
                throw new Exception();
            });
        return this;
    }

    public void VerifyReportingToConveyors(string hu ="", string dropStation = "", string source = "", Times? times = null)
    {
        times ??= Times.Once();
        if (times == Times.Never())
        {
            _communicator.Verify(x => x.ReportPalletDrop(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), (Times)times);
        }
        _communicator.Verify(x => x.ReportPalletDrop(source, dropStation, hu), (Times)times);
    }
}