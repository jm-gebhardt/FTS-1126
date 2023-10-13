using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvPositionRegistry> _agvPositionRegistryMock = new();


    public GetAgvPositionsRequestHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvPositionRegistryMock.Object);
        });
    }

    public GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvPositions(ICollection<TransportOrder.Domain.Models.Agv.AgvPositionStatus> positions)
    {
        _agvPositionRegistryMock.Setup(x => x.GetSelectedAgvsPositions(It.IsAny<ICollection<Guid>>()))
            .Returns((ICollection<Guid> ids) => positions.Where(p => ids.Contains(p.CorrelationId)).ToList());
        _agvPositionRegistryMock.Setup(x => x.GetPosition(It.IsAny<Guid>()))
            .Returns((Guid id) => positions.SingleOrDefault(p => p.CorrelationId == id));
        return this;
    }

    public new GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}