using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    private readonly Mock<IAgvErrorRegistry> _agvErrorRegistry = new();

    protected override void ConfigureServices(IServiceCollection collection)
    {
        collection.AddTransient(_ => _agvErrorRegistry.Object);
        _agvErrorRegistry.Setup(x => x.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).Verifiable();
    }

    public void ErrorShouldBeSaved(Times howManyTimes)
    {
        _agvErrorRegistry.Verify(x => x.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), howManyTimes);
    }
}