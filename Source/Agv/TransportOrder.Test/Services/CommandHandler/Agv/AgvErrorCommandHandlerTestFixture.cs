using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    public readonly Mock<IAgvDb> AgvDbMock = new();
    private readonly List<AgvError> _errors = new();

    public AgvErrorCommandHandlerTestFixture()
    {
        AgvDbMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(() => _errors);
        AgvDbMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(() => _errors);
        AgvDbMock.Setup(x => x.Errors.AddAndUpdateAgvErrorAsync(It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).Callback((List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid agvCorrelationId) =>
        {
            _errors.RemoveAll(e => e.AgvCorrelationId == agvCorrelationId && resolvedErrors.Any(r => r.CorrelationId == e.CorrelationId));
            _errors.AddRange(newErrors);
        });
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvErrorCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvDbMock.Object);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        AgvDbMock.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithActiveAgvErrors(IReadOnlyList<AgvError> agvErrors)
    {
        _errors.AddRange(agvErrors);
        return this;
    }
}