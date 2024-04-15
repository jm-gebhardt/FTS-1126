using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.Agv;

public class GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvPositionRepository> _agvPositionRepositoryMock = new();

    public GetAgvPositionsRequestHandlerTestFixture()
    {
        AddServices(s => { s.AddTransient(_ => _agvPositionRepositoryMock.Object); });
    }

    public GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> WithAgvPositions(ICollection<AgvPositionStatus> positions)
    {
        _agvPositionRepositoryMock.Setup(x => x.GetSelectedAgvsPositions(It.IsAny<ICollection<Guid>>()))
            .Returns((ICollection<Guid> ids) => positions.Where(p => ids.Contains(p.CorrelationId)).ToList());
        _agvPositionRepositoryMock.Setup(x => x.GetPosition(It.IsAny<Guid>()))
            .Returns((Guid id) => positions.SingleOrDefault(p => p.CorrelationId == id));
        return this;
    }

    public new GetAgvPositionsRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}