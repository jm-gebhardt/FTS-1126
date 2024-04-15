using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    private bool _addOrUpdateAgvErrorStatusReturnValue;
    public readonly Mock<IAgvErrorRepository> AgvErrorRepository = new();

    public AgvErrorCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvErrorCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvErrorRepository.Object);
        AgvErrorRepository.Setup(x => x.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).ReturnsAsync(() => _addOrUpdateAgvErrorStatusReturnValue);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAddOrUpdateAgvErrorStatusReturnValue(bool returnValue)
    {
        _addOrUpdateAgvErrorStatusReturnValue = returnValue;
        return this;
    }
}