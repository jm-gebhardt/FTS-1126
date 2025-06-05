using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public class ChooseAgvActionGroupTestFixture<TChooser> : FixtureBase
    where TChooser : class, IChooseAgvActionGroupService
{
    public TChooser Service = null!;

    private List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> NotStartedActionGroups { get; } = new();

    private List<string> LockedNodes { get; } = new();

    private Mock<IRouteService> RouteService { get; } = new();

    private Mock<IActionGroupRepository> AgvActionRepository { get; } = new();

    private Mock<INodeStateRepository> NodeStateRepository { get; } = new();

    private Mock<IQueueRepository> QueueRepository { get; } = new();

    private Mock<IQueueManager> QueueManager { get; } = new();

    public ChooseAgvActionGroupTestFixture(bool realRouteService = false)
    {
        AgvActionRepository.Setup(x => x.GetNotStartedActionGroupsAsync(It.IsAny<Guid>())).ReturnsAsync(NotStartedActionGroups);
        NodeStateRepository.Setup(n => n.GetLockedNodes()).ReturnsAsync(LockedNodes);
        AddServices(s =>
        {
            if (realRouteService)
            {
                s.AddTransient<IRouteService>(_ => new RouteService(new Mock<IGraphRepository>().Object, new Mock<IRouteFinder>().Object, new Mock<ILogger<RouteService>>().Object));
            }
            else
            {
                s.AddTransient(_ => RouteService.Object);
            }
            s.AddTransient(_ => AgvActionRepository.Object);
            s.AddTransient(_ => QueueRepository.Object);
            s.AddTransient(_ => QueueManager.Object);
            s.AddTransient(_ => NodeStateRepository.Object);
            s.AddSingleton<IChooseAgvActionGroupService, TChooser>();
            RegisterLoggerMock<TChooser>(s);
        });
        QueueRepository.Setup(x => x.GetBlockedQueueNodes()).ReturnsAsync([]);
    }

    private void RegisterLoggerMock<T>(IServiceCollection services) where T : class
    {
        services.AddTransient<ILogger<T>>(_ => new Mock<ILogger<T>>().Object);
    }

    public new ChooseAgvActionGroupTestFixture<TChooser> Build()
    {
        base.Build();
        Service = (TChooser)Provider.GetRequiredService<IChooseAgvActionGroupService>();
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithAgvHasRouteToDestination(TransportOrder.Domain.Models.Agv.Agv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, It.IsAny<string>(), It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);
        RouteService.Setup(x => x.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<ICollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, Array.Empty<string>(), Array.Empty<Guid>()));
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithAgvHasRouteToDestination(TransportOrder.Domain.Models.Agv.Agv agv, string sourceNodeId, string destinationNodeId, TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup, float? routeCosts = null)
    {
        RouteService.Setup(x => x.GetCostsAsync(agv, destinationNodeId, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsAsync(agv, sourceNodeId, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetCostsForFirstActionAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);
        RouteService.Setup(x => x.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, It.IsAny<AgvData>(), It.IsAny<ICollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(routeCosts);

        RouteService.Setup(x => x.GetRouteAsync(agv, It.IsAny<string>(), null, It.IsAny<AgvData>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Route(1f, Array.Empty<string>(), Array.Empty<Guid>()));
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithNotStartedActionGroup(TransportOrder.Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        NotStartedActionGroups.Add(actionGroup);
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithLockedNodes(List<string> lockedNodeIds)
    {
        LockedNodes.AddRange(lockedNodeIds);
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> GetCreatedChargingActionGroupForAgvAsync(TransportOrder.Domain.Models.ActionGroup.ActionGroup chargingActionGroup)
    {
        AgvActionRepository.Setup(x => x.GetCreatedChargingActionGroupForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(chargingActionGroup);
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithAgvAtQueueHead(List<Guid> agvsAtQueueHead)
    {
        QueueManager.Setup(x => x.GetAgvsAtQueueHeads()).ReturnsAsync(agvsAtQueueHead);
        return this;
    }

    public ChooseAgvActionGroupTestFixture<TChooser> WithQueue(Queue queue)
    {
        QueueRepository.Setup(x => x.GetQueueForAgv(It.IsAny<Guid>())).ReturnsAsync(queue);
        return this;
    }
}