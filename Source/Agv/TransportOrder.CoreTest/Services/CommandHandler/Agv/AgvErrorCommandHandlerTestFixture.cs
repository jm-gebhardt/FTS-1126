using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    private bool _addOrUpdateAgvErrorStatusReturnValue;
    public readonly Mock<IAgvErrorRepository> AgvErrorRepository = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => AgvErrorRepository.Object);
        AgvErrorRepository.Setup(x => x.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).ReturnsAsync(() => _addOrUpdateAgvErrorStatusReturnValue);
    }

    public AgvErrorCommandHandlerTestFixture WithAddOrUpdateAgvErrorStatusReturnValue(bool returnValue)
    {
        _addOrUpdateAgvErrorStatusReturnValue = returnValue;
        return this;
    }
}