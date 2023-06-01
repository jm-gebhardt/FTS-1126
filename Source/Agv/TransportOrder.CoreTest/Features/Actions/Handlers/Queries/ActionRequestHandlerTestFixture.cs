using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

public class ActionRequestHandlerTestFixture<TRequestClient, TConsumer>  : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    public readonly Mock<IActionGroupRegistry> ActionGroupRegistry = new();
    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public ActionRequestHandlerTestFixture()
    {
        AddServices(s => s.AddTransient<IActionGroupRegistry>(_ => ActionGroupRegistry.Object));
        AddServices(s => s.AddTransient<IMapper>(_ => Mock.Of<IMapper>()));
    }
    
    public ActionRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroup(ActionGroup[] actionGroups)
    {
        ActionGroupRegistry
            .Setup(x => x.GetActionGroupsAsync(AllActionGroupStates, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
            .ReturnsAsync(actionGroups);

        return this;
    }

    public new ActionRequestHandlerTestFixture<TRequestClient, TConsumer> Build()
    {
        base.Build();
        return this;
    }
}