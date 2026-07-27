using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class SendAgvToChargerRequestHandlerTestFixture : MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler>
{
    private readonly Mock<IAgvDb> _agvDb = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<INodeRepository> _nodeRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    public SendAgvToChargerRequestHandlerTestFixture()
    {
        Build();
        _stationRepository.Setup(x => x.GetStation(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Station());
    }

    public Mock<IChargingPointManager> ChargingPointManager { get; } = new();

    protected override MassTransitConsumerTestFixture<SendAgvToChargerRequestHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _agvDb.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);
        collection.AddTransient(_ => _nodeRepository.Object);
        collection.AddTransient(_ => ChargingPointManager.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _, CancellationToken _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithAgv(TransportOrder.Domain.Models.Agv.Agv agv)
    {
        _agvDb.AddAgvs(agv);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithCurrentAction(AgvAction? action)
    {
        _actionGroupRepository.Setup(x => x.GetCurrentAgvActionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(action);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture ReserveChargerReturns(GraphNode chargingNode)
    {
        ChargingPointManager.Setup(x => x.ReserveChargerAsync(It.IsAny<TransportOrder.Domain.Models.Agv.Agv>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(chargingNode);
        return this;
    }

    public SendAgvToChargerRequestHandlerTestFixture WithNodeLock(NodeLock nodeLock)
    {
        _nodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GraphNode{Id = nodeLock.NodeId, Lock = nodeLock});
        return this;
    }
}