using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

public class ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> : MediatorTestFixture<TRequestClient, TConsumer, TConsumer>
    where TRequestClient : class
    where TConsumer : class, IConsumer
{
    private readonly ActionGroupRetrievalServiceTestFixture _actionGroupRetrievalServiceTestFixture = new();
    private readonly ActionGroupRepositoryTestFixture _actionGroupRepositoryFixture = new();
    private readonly EfAgvGraphRepositoryTestFixture _graphRepositoryTestFixture = new();
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
        correlationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId;
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
            await _graphRepositoryTestFixture.AddAsync(_efGraph);
        }
        await _actionGroupRepositoryFixture.BuildAsync();
        AddServices(s => s.AddTransient<IActionGroupRetrievalService>(_ => _actionGroupRetrievalServiceTestFixture.ActionGroupRetrievalService));
        AddServices(s => s.AddTransient<IActionGroupRepository>(_ => _actionGroupRepositoryFixture.ActionGroupRepository));
        AddServices(s => s.AddTransient<IGraphRepository>(_ => _graphRepositoryTestFixture.Repository));

        await base.BuildAsync();
        Build();

        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeFailedAbortAction()
    {
        AddConsumers<AbortActionGroupFailedRequestHandler>();
        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggeredAbortAction()
    {
        AddConsumers<AbortActionGroupSuccessRequestHandler>();
        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggeredResetAction()
    {
        AddConsumers<SuccessRequestHandlerMock<ResetActionGroup>>();
        return this;
    }

    public Guid GetExistingNode()
    {
        return (Guid) _efGraph?.Nodes.First().Id!;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggeredActionCompletion()
    {
        AddConsumers<SuccessRequestHandlerMock<CompleteActionOfActionGroup>>();
        return this;
    }
}