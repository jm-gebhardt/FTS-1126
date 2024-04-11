using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Configurations;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IAgvDb> _agvDb = new();

    private readonly Mock<IActionGroupRetrievalService> _actionGroupRepository = new();

    public AgvSettingsReadModel AgvSettings => EfAgvSettingsProvider.DefaultSettings;

    protected override IEnumerable<Profile> GetProfiles() => [new AgvProfile()];

    public AgvRequestsHandlerTestFixture(IBus? bus = null)
    {
        WithEmptyGraphRepository();
        AddServices(s =>
        {
            s.AddTransient(_ => _agvDb.Object);
            s.AddTransient(_ => _actionGroupRepository.Object);
            s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            if (bus != null)
            {
                s.AddTransient(_ => bus);
            }
            s.AddFakeSettings(() => AgvSettings);
        });

        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        _agvDb.Setup(x => x.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
        _agvDb.Setup(x => x.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync(AgvGenerator.GenerateIdleAgv());
        _agvDb.Setup(x => x.AgvSettings.AddOrUpdateSettingsForAgvGroup(It.IsAny<AgvSettingsReadModel>(), It.IsAny<GroupType>(), It.IsAny<string>())).Verifiable();
        _agvDb.Setup(x => x.AgvSettings.ResetSettingPropertyToDefaultAsync(It.IsAny<Guid>(), It.IsAny<string>())).Verifiable();
    }

    protected override void AddAutoMapper(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyGraphRepository()
    {
        _agvDb.Setup(r => r.Graphs.GetAgvGraphAsync(It.IsAny<Guid>())).Returns((Guid correlationId) => throw new AgvGraphNotDefinedException(correlationId));
        _agvDb.Setup(r => r.Graphs.GetSelectedAgvGraphsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<AgvGraph>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvRepository()
    {
        _agvDb.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.GetAgvAsync(It.IsAny<Guid>())).ThrowsAsync(new AgvNotFoundException(It.IsAny<Guid>()));
        _agvDb.Setup(r => r.Agvs.Count).Returns(0);
        _agvDb.Setup(r => r.Agvs.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        _agvDb.Setup(r => r.Agvs.GetAllAgvsWithGraphAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.GetChargingAgvsWithMinBatteryLevelAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.GetIdleAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.GetReadyAgvsAsync(It.IsAny<int>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        _agvDb.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvDataRepository()
    {
        _agvDb.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync((AgvData?)null);
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
        _agvDb.AddAgvsToMockRepository(agvs);
        _actionGroupRepository.Setup(r => r.GetCurrentActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup.ConvertFromActionGroupWithFakeNodeType()));
        _actionGroupRepository.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()));
        _actionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()));
        _agvDb.Setup(r => r.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            try
            {
                return agvs.Single(a => a.CorrelationId == c);
            }
            catch (InvalidOperationException)
            {
                throw new AgvNotFoundException(c);
            }
        });
        _agvDb.Setup(a => a.Agvs.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Callback((ICollection<Guid> ids) =>
        {
            List<Guid> notExistingAgvs = ids.Where(id => agvs.All(a => a.CorrelationId != id)).ToList();
            if (notExistingAgvs.Any())
            {
                throw new AgvsNotFoundException(notExistingAgvs);
            }
        });
        _agvDb.Setup(r => r.Agvs.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        _actionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        _actionGroupRepository.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        _agvDb.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> ids) => agvs.Where(a => ids.Contains(a.CorrelationId)).ToList());
        IEnumerable<AgvData> specifications = agvs.Select((a, _) => AgvDataGenerator.GenerateAgvData(correlationId: a.CorrelationId));

        _agvDb.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => specifications.Where(s => ids.Contains(s.CorrelationId)).ToList());

        Dictionary<Guid, GraphNode?> nodes = agvs.ToDictionary(a => a.CorrelationId,
            a => a.AgvStatus!.LastNodeId != null ? new GraphNode(a.AgvStatus!.LastNodeId!) { Id = a.AgvStatus!.LastNodeId!, Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f } : null);

        _agvDb.Setup(x => x.Graphs.GetLastNodesForAgvsAsync(It.IsAny<Dictionary<Guid, string?>>())).ReturnsAsync(() => nodes);

        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        _actionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv }, actionGroup);
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