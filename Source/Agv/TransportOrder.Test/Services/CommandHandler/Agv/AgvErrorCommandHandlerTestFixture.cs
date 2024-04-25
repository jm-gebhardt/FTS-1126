using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    private (bool, List<AgvError>) _addOrUpdateAgvErrorStatusReturnValue;
    public readonly Mock<IAgvDb> AgvDbMock = new();

    public AgvErrorCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvErrorCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvDbMock.Object);
        AgvDbMock.Setup(x => x.Errors.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).ReturnsAsync(() => _addOrUpdateAgvErrorStatusReturnValue);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        AgvDbMock.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAddOrUpdateAgvErrorStatusReturnValue(bool returnValue, List<AgvError> newErrors)
    {
        _addOrUpdateAgvErrorStatusReturnValue = (returnValue, newErrors);
        return this;
    }
}