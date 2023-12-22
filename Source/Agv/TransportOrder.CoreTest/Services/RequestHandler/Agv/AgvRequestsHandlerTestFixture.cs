using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

public class AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly Mock<IGraphRepository> _agvGraphRepository = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IAgvRepository> AgvRepository = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IActionGroupRetrievalService> ActionGroupRepository = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IAgvErrorRepository> AgvErrorRepository = new();

    private readonly Mock<IAgvDataRepository> _agvDataRepositoryMock = new();

    public AgvRequestsHandlerTestFixture(IBus? bus = null)
    {
        WithEmptyGraphRepository();
        AddServices(s =>
        {
            s.AddTransient(_ => AgvRepository.Object);
            s.AddTransient(_ => ActionGroupRepository.Object);
            s.AddTransient(_ => Mock.Of<IAgvDataRepository>());
            s.AddTransient(_ => MappingHelper.GetMapper(new AgvProfile()));
            s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            s.AddTransient(_ => _agvGraphRepository.Object);
            s.AddTransient(_ => AgvErrorRepository.Object);
            s.AddTransient(_ => _agvDataRepositoryMock.Object);
            if (bus != null)
            {
                s.AddTransient(_ => bus);
            }
        });

        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyGraphRepository()
    {
        _agvGraphRepository.Setup(r => r.GetAgvGraphAsync(It.IsAny<Guid>())).Returns((Guid correlationId) => throw new AgvGraphNotDefinedException(correlationId));
        _agvGraphRepository.Setup(r => r.GetSelectedAgvGraphsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<AgvGraph>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvRepository()
    {
        AgvRepository.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.GetAgvAsync(It.IsAny<Guid>())).ThrowsAsync(new AgvNotFoundException(It.IsAny<Guid>()));
        AgvRepository.Setup(r => r.Count).Returns(0);
        AgvRepository.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        AgvRepository.Setup(r => r.GetAllAgvsWithGraphAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.GetChargingAgvsWithGoodBatteryLevelAsync(It.IsAny<int>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.GetIdleAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.GetReadyAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRepository.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvErrorRepository()
    {
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRepository.Setup(x => x.GetErrorsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRepository.Setup(x => x.GetErrorsAsync(It.IsAny<IList<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<AgvError>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        AgvRepository.AddAgvsToMockRepository(agvs);
        ActionGroupRepository.Setup(r => r.GetCurrentActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup.ConvertFromActionGroupWithFakeNodeType()));
        ActionGroupRepository.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()));
        ActionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()));
        AgvRepository.Setup(r => r.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
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
        AgvRepository.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        ActionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        ActionGroupRepository.Setup(r => r.GetCurrentAgvActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        AgvRepository.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> ids) => agvs.Where(a => ids.Contains(a.CorrelationId)).ToList());
        IEnumerable<AgvData> specifications = agvs.Select((a, i) => new AgvData(a.CorrelationId, "Karis", "BF1", "ipv4 address",
            1.8, 120.0, 0.6, 0.8, 0.7, 0.08726646, i, 2.5, 1000, Array.Empty<AgvData.Sensor>()));

        _agvDataRepositoryMock.Setup(x => x.GetSelectedAgvData(It.IsAny<ICollection<Guid>>())).ReturnsAsync((ICollection<Guid> ids) => specifications.Where(s => ids.Contains(s.CorrelationId)).ToList());
 
        Dictionary<Guid, GraphNode?> nodes = agvs.ToDictionary(a => a.CorrelationId, 
            a => a.AgvStatus!.LastNodeId != null ? new GraphNode(a.AgvStatus!.LastNodeId!) { Id = a.AgvStatus!.LastNodeId! ,Type = AgvGraph.NodeType.WayPoint, X = 1.0f, Y = 2.0f, Z = 0.0f, Theta = 0.0f} : null);
 
        _agvGraphRepository.Setup(x => x.GetLastNodesForAgvsAsync(It.IsAny<Dictionary<Guid, string?>>())).ReturnsAsync(() => nodes);
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        ActionGroupRepository.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv }, actionGroup);
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithActiveAgvErrors(Guid agvCorrelationId, List<AgvError> agvErrors)
    {
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(agvCorrelationId)).ReturnsAsync(agvErrors);
        AgvErrorRepository.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(agvErrors);
        AgvErrorRepository.Setup(x => x.GetErrorsAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(agvErrors.AsReadOnly);
        AgvErrorRepository.Setup(x => x.GetErrorsAsync(It.IsAny<IList<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(agvErrors.AsReadOnly);
        return this;
    }

    public new AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}