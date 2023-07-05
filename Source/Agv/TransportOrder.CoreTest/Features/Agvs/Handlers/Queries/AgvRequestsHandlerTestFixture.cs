using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
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
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IAgvRegistry> AgvRegistry = new();

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Mock<IActionGroupRegistry> ActionGroupRegistry = new();

    public AgvRequestsHandlerTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => AgvRegistry.Object);
            s.AddTransient(_ => ActionGroupRegistry.Object);
            s.AddTransient(_ => Mock.Of<IAgvDataRegistry>());
            s.AddTransient(_ => MappingHelper.GetMapper(new List<Profile> {new AgvProfile(), new ActionProfile()}));
            s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgvs(IList<IAgv> agvs, ActionGroup? actionGroup = null)
    {
        AgvRegistry.Setup(s => s.GetAllAgvs()).Returns(agvs.AsQueryable());
        ActionGroupRegistry.Setup(r => r.GetCurrentAssignedActionGroupsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(agvs.ToDictionary(a => a.CorrelationId, _ => actionGroup));
        return this;
    }

    public AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> WithAgv(IAgv agv)
    {
        AgvRegistry.Setup(r => r.GetAgv(agv.CorrelationId)).Returns(agv);
        return this;
    }

    public new AgvRequestsHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}