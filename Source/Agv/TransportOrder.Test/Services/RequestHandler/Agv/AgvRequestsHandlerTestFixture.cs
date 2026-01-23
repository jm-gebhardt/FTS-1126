using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

public class AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, MockConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();

    private readonly Mock<IActionGroupRetrievalService> _actionGroupRetrievalService = new();
    private readonly Mock<IActionGroupRepository> _actionGroupRepository = new();
    private readonly Mock<INodeStateRepository> _nodeStateRepositoryMock = new();
    private readonly Mock<ILogEntryRepository> _logEntryRepository = new();

    public AgvSettingsReadModel AgvSettings => EfAgvSettingsProvider.DefaultSettings;

    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile()];

    public AgvRequestsHandlerTestFixture(IBus? bus = null)
    {
        WithEmptyGraphRepository();
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _actionGroupRetrievalService.Object);
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddTransient(_ => _nodeStateRepositoryMock.Object);
            s.AddTransient(_ => _logEntryRepository.Object);
            s.AddTransient<ILoggingService, LoggingService>();
            if (bus != null)
            {
                var descriptor = new ServiceDescriptor(typeof(IBus), _ => bus, ServiceLifetime.Singleton);
                s.Replace(descriptor);
            }
            s.AddFakeSettings(() => AgvSettings);
        });

        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
        _agvDb.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(AgvGenerator.GenerateIdleAgv());
        _agvDb.Setup(x => x.AgvSettings.AddOrUpdateSettingsForAgvGroup(It.IsAny<AgvSettingsReadModel>(), It.IsAny<GroupType>(), It.IsAny<string>())).Verifiable();
        _agvDb.Setup(x => x.AgvSettings.ResetSettingPropertyToDefaultAsync(It.IsAny<Guid>(), It.IsAny<string>())).Verifiable();
        _logEntryRepository.Setup(x => x.AddAsync(It.IsAny<Categories>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, object?>>())).ReturnsAsync((Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> _) => new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId));
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithLastMaintenance(TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog? maintenanceLog = null)
    {
        _agvDb.Setup(x => x.MaintenanceLogs.GetLatestMaintenanceLogForAgvAsync(It.IsAny<Guid>())).ReturnsAsync(maintenanceLog);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithLastMaintenances(ICollection<TransportOrder.Domain.Models.MaintenanceLog.MaintenanceLog> maintenanceLogs)
    {
        _agvDb.Setup(x => x.MaintenanceLogs.GetLatestMaintenanceLogsForAgvsAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync(
            maintenanceLogs
                .GroupBy(log => log.AgvCorrelationId)
                .ToDictionary(
                    group => group.Key,
                    group => group.FirstOrDefault()
                ));
        return this;
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyGraphRepository()
    {
        _agvDb.Setup(r => r.Graphs.GetAgvGraphAsync(It.IsAny<Guid>())).Returns((Guid correlationId) => throw new AgvGraphNotDefinedException(correlationId));
        _agvDb.Setup(r => r.Graphs.GetSelectedAgvGraphsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<AgvGraph>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvRepository()
    {
        _agvDb.AddAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvDataRepository()
    {
        _agvDb.AddAgvData((AgvData?) null);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithNodesStates(ICollection<NodeLock> nodeLocks)
    {
        _nodeStateRepositoryMock.Setup(x => x.GetNodeLocksForAgv(It.IsAny<Guid>())).ReturnsAsync(nodeLocks);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithPostion(AgvPositionStatus position)
    {
        _agvDb.AddPosition(position);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvErrorRepository()
    {
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.Errors.GetErrorsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.Errors.GetErrorsAsync(It.IsAny<IList<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<AgvError>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        _agvDb.AddAgvs(agvs);
        _actionGroupRetrievalService.Setup(r => r.GetCurrentActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup.ConvertFromActionGroupWithFakeNodeType()));
        _actionGroupRetrievalService.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()));
        _actionGroupRetrievalService.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()));
        _actionGroupRetrievalService.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        _actionGroupRetrievalService.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        IEnumerable<AgvData> specifications = agvs.Select((a, _) => AgvDataGenerator.GenerateAgvData(correlationId: a.CorrelationId));

        _agvDb.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => specifications.Where(s => ids.Contains(s.AgvCorrelationId)).ToList());

        Dictionary<Guid, GraphNode?> nodes = agvs.ToDictionary(a => a.CorrelationId,
            a => a.AgvStatus!.LastNodeId != null ? NodeGenerator.GenerateNode(nodeName: a.AgvStatus!.LastNodeId!.ToString()!, x: 1.0f, y: 2.0f, z: 0.0f, theta: 0.0f, nodeType: AgvGraph.NodeType.WayPoint) : null);

        _agvDb.Setup(x => x.Graphs.GetLastNodesForAgvsAsync(It.IsAny<Dictionary<Guid, Guid?>>())).ReturnsAsync(() => nodes);

        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> actionGroups = [];
        if (actionGroup != null)
        {
            actionGroups = [actionGroup];
        }
        _actionGroupRepository.Setup(x => x.GetActionGroupsAsync(It.IsAny<ActionGroupState>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<bool?>())).ReturnsAsync(actionGroups);
        _actionGroupRetrievalService.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return WithAgvs([agv], actionGroup);
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithOnlineAgv(TransportOrder.Domain.Models.Agv.Agv? agv)
    {
        _agvDb.Setup(x => x.Agvs.GetAnyOnlineAgv()).ReturnsAsync(agv);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithActiveAgvErrors(Guid agvCorrelationId, List<AgvError> agvErrors)
    {
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(agvCorrelationId)).ReturnsAsync(agvErrors);
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(agvErrors);
        _agvDb.Setup(x => x.Errors.GetErrorsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(agvErrors.AsReadOnly);
        _agvDb.Setup(x => x.Errors.GetErrorsAsync(It.IsAny<IList<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(agvErrors.AsReadOnly);
        return this;
    }

    public new AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithSettingsGroup(AgvSettingsGroup group)
    {
        _agvDb.Setup(x => x.AgvSettings.GetSettingsGroupForAgv(It.IsAny<TransportOrder.Domain.Models.Agv.Agv?>())).ReturnsAsync(group);
        return this;
    }

    public void VerifyAgvSettingsUpdated(GroupType groupType, string groupIdentifier, AgvSettingsReadModel settings)
    {
        _agvDb.Verify(x => x.AgvSettings.AddOrUpdateSettingsForAgvGroup(settings, groupType, groupIdentifier), Times.Once);
    }

    public void VerifyAgvSettingsPropertyReset(Guid agvGroup, string propertyName)
    {
        _agvDb.Verify(x => x.AgvSettings.ResetSettingPropertyToDefaultAsync(agvGroup, propertyName), Times.Once);
    }
}