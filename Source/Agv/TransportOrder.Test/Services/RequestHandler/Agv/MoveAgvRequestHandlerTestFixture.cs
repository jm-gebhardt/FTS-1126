using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IRestrictedAreaRepository> _restrictedAreaRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    public Mock<IParkPointService> ParkPointService { get; } = new();

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    public MoveAgvRequestHandlerTestFixture()
    {
        _agvDb.SetupNodeFallbacks();
    }

    protected override IEnumerable<Profile> GetProfiles() => [new LogEntryProfile()];

    public new MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> BuildAsync()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _nodeStateRepository.Object);
            s.AddTransient(_ => _stationRepository.Object);
            s.AddTransient(_ => ParkPointService.Object);
            s.AddTransient(_ => ChargingPointManager.Object);
            s.AddTransient(_ => _restrictedAreaRepository.Object);
            s.AddTransient(_ => _logEntryRepository.Object);
            s.AddTransient<ILoggingService, LoggingService>();
            s.AddTransient(_ => _actionGroupRepository.Object);
        });

        AddConsumers<SuccessQueueActionGroupRequestHandler>();
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>())).Verifiable();
        _nodeStateRepository.Setup(x => x.GetNodeStateAsync(It.Is<Guid>(y => y == "target".ToGuid()))).ReturnsAsync(new TransportOrder.Domain.Models.Node.NodeLock("target".ToGraphNode(), TransportOrder.Domain.Models.Node.NodeLockState.Free, false, Guid.NewGuid(), DateTime.UtcNow));
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new Station());
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>())).ReturnsAsync(new Station());
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([]);
        base.Build();
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDb.AddAgvs(agvs);
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithNode(params GraphNode?[] nodes)
    {
        _agvDb.AddNode(nodes);
        return this;
    }

    public MoveAgvRequestHandlerTestFixture<TRequestClient, TConsumer> WithInsideRestrictedArea(bool isInside)
    {
        _restrictedAreaRepository.Setup(x => x.IsPointInRestrictedArea(It.IsAny<float>(), It.IsAny<float>())).ReturnsAsync(isInside);
        return this;
    }

    public void VerifyNodeWasReserved(AgvGraph.NodeType nodeType, GraphNode targetNode, Guid agvId)
    {
        switch (nodeType)
        {
            case AgvGraph.NodeType.Charge:
                ChargingPointManager.Verify(x => x.ReserveChargerAsync(It.Is<GraphNode>(n => n == targetNode), It.Is<Guid>(a => a == agvId), true), Times.Once());
                break;

            case AgvGraph.NodeType.Park:
                ParkPointService.Verify(x => x.ReserveParkAsync(It.Is<Guid>(n => n == targetNode.Id), It.Is<Guid>(a => a == agvId), true), Times.Once());
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}