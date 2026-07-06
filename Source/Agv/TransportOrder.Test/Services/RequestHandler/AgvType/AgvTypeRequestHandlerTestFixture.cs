using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.AgvType;

public class AgvTypeRequestHandlerTestFixture<TRequest, TConsumer> : MediatorTestFixture<TRequest, TConsumer, MockConsumer>
    where TRequest : class
    where TConsumer : class, IConsumer
{
    public readonly Mock<IAgvDb> AgvDb = new();
    private readonly List<TransportOrder.Domain.Models.AgvTypes.AgvType> _agvTypes = new();

    public AgvTypeRequestHandlerTestFixture()
    {
        SetupAgvTypeRepositoryMock();
        AddServices(s => s.AddTransient<IAgvDb>(_ => AgvDb.Object));
    }

    private void SetupAgvTypeRepositoryMock()
    {
        AgvDb.Setup(x => x.AgvTypes.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => _agvTypes.Any(a => a.Id == id));
        AgvDb.Setup(x => x.AgvTypes.GetAgvTypeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _agvTypes.SingleOrDefault(a => a.Id == id) ?? throw new AgvTypeNotFoundException(id));
        AgvDb.Setup(x => x.AgvTypes.GetAllAgvTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => _agvTypes.ToList());
    }

    public AgvTypeRequestHandlerTestFixture<TRequest, TConsumer> WithAgvTypes(params TransportOrder.Domain.Models.AgvTypes.AgvType[] agvTypes)
    {
        _agvTypes.AddRange(agvTypes);
        return this;
    }

    public override AgvTypeRequestHandlerTestFixture<TRequest, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}
