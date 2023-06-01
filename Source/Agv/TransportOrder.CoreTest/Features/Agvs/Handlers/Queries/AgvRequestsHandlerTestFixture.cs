using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

public class AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    public readonly Mock<IAgvRegistry> AgvRegistry = new();
    public readonly Mock<IActionGroupRegistry> ActionGroupRegistry = new();

    public AgvRequestsHandlerTestFixture()
    {
        AddServices(s => s.AddTransient(_ => AgvRegistry.Object));
        AddServices(s => s.AddTransient(_ => ActionGroupRegistry.Object));
        AddServices(s => s.AddTransient(_ => Mock.Of<IAgvDataRegistry>()));
        AddServices(s => s.AddTransient(_ => Mock.Of<IMapper>()));
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(IList<IAgv> agvs, ActionGroup? actionGroup = null)
    {
        AgvRegistry.Setup(s => s.GetAllAgvs()).Returns(agvs.AsQueryable());
        ActionGroupRegistry.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup));
        return this;
    }

    public new AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}