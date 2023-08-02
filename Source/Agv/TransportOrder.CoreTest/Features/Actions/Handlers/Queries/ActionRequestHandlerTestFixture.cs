using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Actions.Handlers.Queries;

// ReSharper disable ConvertConstructorToMemberInitializers
public class ActionRequestHandlerTestFixture<TRequestClient, TConsumer>  : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly ActionGroupRegistryTestFixture _actionGroupRegistryFixture = new();
    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public ActionRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroups(IEnumerable<ActionGroup> actionGroups)
    {
        _actionGroupRegistryFixture.WithActionGroups(actionGroups);
        return this;
    }

    public async Task<ActionRequestHandlerTestFixture<TRequestClient, TConsumer>> BuildAsync()
    {
        await _actionGroupRegistryFixture.BuildAsync();
        AddServices(s => s.AddTransient<IActionGroupRegistry>(_ => _actionGroupRegistryFixture.ActionGroupRegistry));
        AddServices(s => s.AddTransient<IMapper>(_ => Mock.Of<IMapper>()));
        base.Build();
        
        return this;
    }
}