using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

public class AgvErrorCommandHandlerTestFixture : MassTransitConsumerTestFixture<AgvErrorCommandHandler>
{
    private (bool, List<AgvError>, List<AgvError>) _addOrUpdateAgvErrorStatusReturnValue;
    public readonly Mock<IAgvDb> AgvDbMock = new();
    private readonly Mock<IMetricCollectionService> _metricCollectorServiceMock = new();

    public AgvErrorCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<AgvErrorCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => AgvDbMock.Object);
        collection.AddTransient(_ => _metricCollectorServiceMock.Object);
        AgvDbMock.Setup(x => x.Errors.AddOrUpdateAgvErrorStatus(It.IsAny<List<AgvError>>(), It.IsAny<Guid>())).ReturnsAsync(() => _addOrUpdateAgvErrorStatusReturnValue);
        _metricCollectorServiceMock.Setup(x => x.CollectSafetyFieldViolationMetrics(It.IsAny<Guid>(), It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>())).Verifiable();
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        AgvDbMock.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(agv);
        return this;
    }

    public AgvErrorCommandHandlerTestFixture WithAddOrUpdateAgvErrorStatusReturnValue(bool returnValue, List<AgvError> newErrors)
    {
        _addOrUpdateAgvErrorStatusReturnValue = (returnValue, newErrors, []);
        return this;
    }

    public void VerifyMetricsCollected(Times times)
    {
        _metricCollectorServiceMock.Verify(x => x.CollectSafetyFieldViolationMetrics(It.IsAny<Guid>(), It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>()), times);
    }
}