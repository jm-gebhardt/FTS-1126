using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.ActionGroup;

public class ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly ActionGroupRetrievalServiceTestFixture _actionGroupRetrievalServiceTestFixture = new();
    private readonly ActionGroupRepositoryTestFixture _actionGroupRepositoryFixture = new();
    private EfAgvGraph? _efGraph;
    private TransportOrder.Domain.Models.ActionGroup.ActionGroup[]? _actionGroups;

    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroups(params TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups)
    {
        _actionGroups = actionGroups;

        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> AddDefaultGraph(Guid correlationId = default)
    {
        correlationId = correlationId == default ? Guid.NewGuid() : correlationId;
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var mapper = GetMapper();
        _efGraph = mapper.Map<EfAgvGraph>(graph);

        return this;
    }

    public new async Task<ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>> BuildAsync()
    {
        if (_actionGroups != null)
        {
            await _actionGroupRetrievalServiceTestFixture.AddRangeAsync(_actionGroups.ToList());
            _actionGroupRepositoryFixture.WithActionGroups(_actionGroups);
        }

        if (_efGraph != null)
        {
            await _actionGroupRetrievalServiceTestFixture.AddAsync(_efGraph);
        }
        await _actionGroupRepositoryFixture.BuildAsync();
        AddServices(s => s.AddTransient<IActionGroupRetrievalService>(_ => _actionGroupRetrievalServiceTestFixture.ActionGroupRetrievalService));
        AddServices(s => s.AddTransient<IActionGroupRepository>(_ => _actionGroupRepositoryFixture.ActionGroupRepository));

        await base.BuildAsync();
        Build();

        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeFailedAbortAction()
    {
        AddConsumers<AbortActionGroupFailedRequestHandler>();
        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggerredAbortAction()
    {
        AddConsumers<AbortActionGroupSuccessRequestHandler>();
        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggerredResetAction()
    {
        AddConsumers<SuccessRequestHandlerMock<ResetActionGroup>>();
        return this;
    }
}