using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Test.Services.EventHandler;

public class MetricsCollectionEventHandlerTestFixture : MassTransitConsumerTestFixture<MetricsCollectionEventHandler>
{
    private readonly Mock<IMetricCollectionService> _metricCollectorServiceMock = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    private static IEnumerable<Profile> Profiles => [new AgvStatusProfile()];

    protected override IEnumerable<Profile> GetProfiles() => Profiles;

    protected override MassTransitConsumerTestFixture<MetricsCollectionEventHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _metricCollectorServiceMock.Object);
        return this;
    }

    public MetricsCollectionEventHandlerTestFixture()
    {
        Build();
        _metricCollectorServiceMock.Setup(x => x.CollectBatteryChargeMetrics(It.IsAny<AgvStatus>())).Verifiable();
        _metricCollectorServiceMock.Setup(x => x.CollectOrderCompletionMetrics(It.IsAny<ActionGroup>(), It.IsAny<Guid?>())).Verifiable();
        _metricCollectorServiceMock.Setup(x => x.CollectStationOrderCompletionMetrics(It.IsAny<ActionType>(), It.IsAny<Guid>())).Verifiable();
        _metricCollectorServiceMock.Setup(x => x.CollectSafetyFieldViolationMetrics(It.IsAny<Guid>(), It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>())).Verifiable();
        _metricCollectorServiceMock.Setup(x => x.CollectJamStateMetrics(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<bool>())).Verifiable();

        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync((ActionGroup?)null);
    }

    public void VerifyJamStateMetricsCollected()
    {
        _metricCollectorServiceMock.Verify(x => x.CollectJamStateMetrics(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Once);
    }

    public void VerifySafetyFieldViolationMetricsCollected()
    {
        _metricCollectorServiceMock.Verify(x => x.CollectSafetyFieldViolationMetrics(It.IsAny<Guid>(), It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>()), Times.Once());
    }

    public void VerifyBatteryMetricsCollected()
    {
        _metricCollectorServiceMock.Verify(x => x.CollectBatteryChargeMetrics(It.IsAny<AgvStatus>()),  Times.Once());
    }

    public void VerifyStationOrderCompletionMetricsCollected(Times callTimes)
    {
        _metricCollectorServiceMock.Verify(x => x.CollectStationOrderCompletionMetrics(It.IsAny<ActionType>(), It.IsAny<Guid>()), callTimes);
    }

    public void VerifyOrderCompletionMetricsCollected(Times callTimes)
    {
        _metricCollectorServiceMock.Verify(x => x.CollectOrderCompletionMetrics(It.IsAny<ActionGroup>(), It.IsAny<Guid?>()), callTimes);
    }

    public MetricsCollectionEventHandlerTestFixture AddActionGroup(ActionGroup actionGroup)
    {
        _actionGroupRepository.Setup(x => x.GetActionGroupAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        _actionGroupRepository.Setup(x => x.GetRequiredActionGroupByActionIdAsync(It.IsAny<Guid>())).ReturnsAsync(actionGroup);
        return this;
    }
}