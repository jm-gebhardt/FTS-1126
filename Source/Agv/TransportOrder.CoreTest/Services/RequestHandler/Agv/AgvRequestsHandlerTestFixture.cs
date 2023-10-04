using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
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
    public readonly Mock<IAgvRegistry> AgvRegistry = new();

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

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(IList<IAgv> agvs, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        AgvRegistry.Setup(s => s.GetAllAgvs()).Returns(agvs.AsQueryable());
        ActionGroupRegistry.Setup(r => r.GetAssignedActionGroupsWithNodesAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup.ConvertFromActionGroupWithFakeNodeType()));
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(IAgv agv, TransportOrder.Domain.Models.ActionGroup.ActionGroup? actionGroup = null)
    {
        AgvRegistry.Setup(r => r.GetAgv(agv.CorrelationId)).Returns(agv);
        AgvRegistry.Setup(r => r.CheckIfAgvExistsAsync(agv.CorrelationId)).ReturnsAsync(true);
        ActionGroupRegistry.Setup(r => r.GetActionGroupWithNodesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(actionGroup.ConvertFromActionGroupWithFakeNodeType());
        return this;
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