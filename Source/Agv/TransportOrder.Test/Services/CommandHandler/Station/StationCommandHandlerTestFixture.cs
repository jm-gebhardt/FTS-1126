using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Station;

public class StationCommandHandlerTestFixture : MassTransitConsumerTestFixture<StationCommandHandler>
{
    public readonly Mock<INodeRepository> NodeRepository = new();
    private readonly Mock<IStationRepository> _stationRepository = new();
    private readonly Mock<IGraphRepository> _graphRepository = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();

    protected override IEnumerable<Profile> GetProfiles() => [new LogEntryProfile()];

    public StationCommandHandlerTestFixture()
    {
        Build();
    }

    protected override MassTransitConsumerTestFixture<StationCommandHandler> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => NodeRepository.Object);
        collection.AddTransient(_ => _stationRepository.Object);
        collection.AddTransient(_ => _graphRepository.Object);
        collection.AddTransient(_ => _actionGroupRepository.Object);

        collection.AddTransient<ILoggingService, LoggingService>();
        collection.AddTransient(_ => _logEntryRepository.Object);
        _stationRepository.Setup(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>())).Returns(Task.CompletedTask).Verifiable();
        NodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(true).Verifiable();
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<IList<ActionGroupState>?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<SortBy>(), It.IsAny<bool>())).ReturnsAsync([]);
        return this;
    }

    public StationCommandHandlerTestFixture WithNode(GraphNode node)
    {
        NodeRepository.Setup(x => x.GetNodeAsync(node.Id)).ReturnsAsync(node);
        NodeRepository.Setup(x => x.GetNodeAsync(It.IsAny<Guid>())).ReturnsAsync(node);
        return this;
    }

    public StationCommandHandlerTestFixture WithStation(string nodeName)
    {
        var station = new TransportOrder.Domain.Models.Station.Station(nodeName, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>());
        var id = station.Id;
        _stationRepository.Setup(x => x.GetStation(id)).ReturnsAsync(station);
        _stationRepository.Setup(x => x.GetStation(nodeName.ToGuid())).ReturnsAsync(station);
        return this;
    }

    public StationCommandHandlerTestFixture WithAgvsRelatedToNodes(ICollection<Guid> agvIds)
    {
        _graphRepository.Setup(x => x.GetAgvIdsRelatedToNodesAsync(It.IsAny<ICollection<GraphNode>>())).ReturnsAsync(agvIds);
        return this;
    }

    public StationCommandHandlerTestFixture WithAgvsGraph()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        _graphRepository.Setup(x => x.GetAgvGraphAsync(It.IsAny<Guid>())).ReturnsAsync(graph);
        return this;
    }

    public void VerifyStationUpdated(Times times)
    {
        _stationRepository.Verify(x => x.Update(It.IsAny<TransportOrder.Domain.Models.Station.Station>()), times);
    }

    public void VerifyStationsAdded(Times times)
    {
        _stationRepository.Verify(x => x.AddStations(It.IsAny<AgvGraph>()), times);
    }
}