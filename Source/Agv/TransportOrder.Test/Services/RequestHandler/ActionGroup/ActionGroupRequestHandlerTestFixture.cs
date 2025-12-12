using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
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
    private readonly EfLogEntryRepositoryTestFixture _logEntryRepositoryTestFixture = new();
    private AgvGraph? _graph;
    private TransportOrder.Domain.Models.ActionGroup.ActionGroup[]? _actionGroups;

    protected override IEnumerable<Profile> GetProfiles() => [new GraphProfile(), new LogEntryProfile(), new ActionProfile()];

    public ActionGroupState[] AllActionGroupStates => Enum.GetValues(typeof(ActionGroupState)).Cast<ActionGroupState>().ToArray();

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithActionGroups(params TransportOrder.Domain.Models.ActionGroup.ActionGroup[] actionGroups)
    {
        _actionGroups = actionGroups;

        return this;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> AddDefaultGraph(Guid correlationId = default, ICollection<GraphNode>? nodes = null)
    {
        correlationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId;
        var graph = AgvGraphGenerator.CreateDefaultGraph(correlationId, nodes);
        _graph = graph;

        return this;
    }

    public new async Task<ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer>> BuildAsync()
    {
        AddServices(s => s.AddTransient<IActionGroupRetrievalService>(_ => _actionGroupRetrievalServiceTestFixture.ActionGroupRetrievalService));
        AddServices(s => s.AddTransient<IActionGroupRepository>(_ => _actionGroupRepositoryFixture.ActionGroupRepository));
        AddServices(s => s.AddTransient<IGraphRepository>(_ => _graphRepositoryTestFixture.Repository));
        AddServices(s => s.AddTransient<ILogEntryRepository>(_ => _logEntryRepositoryTestFixture.Repository));
        AddServices(s => s.AddTransient<ILoggingService, LoggingService>());

        if (_actionGroups != null)
        {
            _actionGroupRetrievalServiceTestFixture.WithActionGroups(_actionGroups.ToList());
            _actionGroupRepositoryFixture.WithActionGroups(_actionGroups);
        }

        Build();
        await base.BuildAsync();

        if (_graph != null)
        {
            await _actionGroupRetrievalServiceTestFixture.AddAsync(_graph);
            await _graphRepositoryTestFixture.AddAsync(_graph);
        }
        
        await _actionGroupRepositoryFixture.BuildAsync();
        await _actionGroupRetrievalServiceTestFixture.BuildAsync();

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

    public Guid GetExistingNode(AgvGraph.NodeType? nodeType = null)
    {
        return (Guid)_graph?.Nodes.First(n => nodeType == null || (AgvGraph.NodeType)n.Type == nodeType).Id!;
    }

    public ActionGroupRequestHandlerTestFixture<TRequestClient, TConsumer> WithFakeTriggeredActionCompletion()
    {
        AddConsumers<SuccessRequestHandlerMock<CompleteActionOfActionGroup>>();
        return this;
    }
}