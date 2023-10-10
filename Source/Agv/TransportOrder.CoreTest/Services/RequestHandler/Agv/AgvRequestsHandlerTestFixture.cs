using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
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
    private readonly Mock<IAgvGraphRegistry> _agvGraphRegistry = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IAgvRepository> AgvRegistry = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IActionGroupRetrievalService> ActionGroupRegistry = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IAgvErrorRegistry> AgvErrorRegistry = new();

    public AgvRequestsHandlerTestFixture(IBus? bus=null)
    {
        WithEmptyGraphRegistry();
        AddServices(s =>
        {
            s.AddTransient(_ => AgvRegistry.Object);
            s.AddTransient(_ => ActionGroupRegistry.Object);
            s.AddTransient(_ => Mock.Of<IAgvDataRegistry>());
            s.AddTransient(_ => MappingHelper.GetMapper(new AgvProfile()));
            s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            s.AddTransient(_ => _agvGraphRegistry.Object);
            s.AddTransient(_ => AgvErrorRegistry.Object);
            if (bus != null)
            {
                s.AddTransient(_ => bus);
            }
        });

        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AgvError>());
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(new List<AgvError>());
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyGraphRegistry()
    {
        _agvGraphRegistry.Setup(r => r.GetAgvGraphAsync(It.IsAny<Guid>())).Returns((Guid correlationId) => throw new AgvGraphNotDefinedException(correlationId));
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithEmptyAgvRepository()
    {
        AgvRegistry.Setup(r => r.GetAllAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRegistry.Setup(r => r.GetAgvAsync(It.IsAny<Guid>())).ThrowsAsync(new AgvNotFoundException(It.IsAny<Guid>()));
        AgvRegistry.Setup(r => r.Count).Returns(0);
        AgvRegistry.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        AgvRegistry.Setup(r => r.GetAllAgvsWithGraphAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRegistry.Setup(r => r.GetChargingAgvsWithGoodBatteryLevelAsync(It.IsAny<int>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRegistry.Setup(r => r.GetIdleAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRegistry.Setup(r => r.GetReadyAgvsAsync()).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        AgvRegistry.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync(new List<TransportOrder.Domain.Models.Agv.Agv>());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs, Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        AgvRegistry.AddAgvsToMockRepository(agvs);
        ActionGroupRegistry.Setup(r => r.GetAssignedActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup.ConvertFromActionGroupWithFakeNodeType()));
        ActionGroupRegistry.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()));
        AgvRegistry.Setup(r => r.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
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
        AgvRegistry.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        ActionGroupRegistry.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(TransportOrder.Domain.Models.Agv.Agv agv, Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        ActionGroupRegistry.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv>{agv}, actionGroup);
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithActiveAgvErrors(Guid agvCorrelationId, IList<AgvError> agvErrors)
    {
        AgvErrorRegistry.Setup(x => x.GetActiveErrorsAsync(agvCorrelationId)).ReturnsAsync(agvErrors);
        return this;
    }

    public new AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}