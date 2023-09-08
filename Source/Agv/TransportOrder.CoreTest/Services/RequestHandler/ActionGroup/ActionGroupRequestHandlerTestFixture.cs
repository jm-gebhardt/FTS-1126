using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Models.Graph;
using TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

// ReSharper disable ConvertConstructorToMemberInitializers
public class ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>  : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly ActionGroupRetrievalServiceTestFixture _actionGroupRetrievalServiceTestFixture = new();
    private readonly ActionGroupRegistryTestFixture _actionGroupRegistryFixture = new();
    private static Mapper GetMapper() => MappingHelper.GetMapper(new GraphProfile());

    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public async Task<ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>> WithActionGroups(global::TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups)
    {
        // ReSharper disable once CoVariantArrayConversion
        await _actionGroupRetrievalServiceTestFixture.AddRangeAsync(actionGroups);
        _actionGroupRegistryFixture.WithActionGroups(actionGroups);
        return this;
    }

    public async Task AddDefaultGraph(Guid correlationId)
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var mapper = GetMapper();
        var efGraph = mapper.Map<EfGraph>(graph);
        await _actionGroupRetrievalServiceTestFixture.AddAsync(efGraph);
    }

    public async Task<ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>> BuildAsync()
    {
        await _actionGroupRegistryFixture.BuildAsync();
        AddServices(s => s.AddTransient<IActionGroupRetrievalService>(_ => _actionGroupRetrievalServiceTestFixture.ActionGroupRetrievalService));
        AddServices(s => s.AddTransient<IActionGroupRegistry>(_ => _actionGroupRegistryFixture.ActionGroupRegistry));
        AddServices(s => s.AddTransient<IMapper>(_ => Mock.Of<IMapper>()));
        base.Build();

        return this;
    }
}