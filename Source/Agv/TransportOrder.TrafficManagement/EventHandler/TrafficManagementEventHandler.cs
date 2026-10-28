using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Events.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Requests.TrafficManagement;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Tick;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;

/// <summary>
/// Consumes incoming routing messages sequentially. The consumer registration in <see cref="Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations.TrafficManagementBusRegistrationExtensions"/>
/// ensures that only one message is processed at a time. Consume all relevant messages for traffic management in this class to avoid concurrency issues
/// (or configure multiple consumers to use the same receive endpoint with a concurrency limit).
/// </summary>
[UsedImplicitly]
internal class TrafficManagementEventHandler(RouteManager routeManager, AgvProvider agvProvider, GraphProvider graphProvider, TrafficManagementTickState tickState, ILogger<TrafficManagementEventHandler> logger) :
    IConsumer<StartRouteToNode>,
    IConsumer<StartRouteWithPick>,
    IConsumer<StartRouteWithDrop>,
    IConsumer<AbortRoute>,
    IConsumer<GetRoute>,
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<AbortAndStartActionCommand>,
    IConsumer<ActionAborted>,
    IConsumer<RouteStepsProcessed>,
    IConsumer<TrafficManagementTick>,
    IConsumer<GraphSegmentAdded>,
    IConsumer<AgvDiscovered>,
    IConsumer<AgvAgvTypeUpdated>,
    IConsumer<AgvTypeAdded>,
    IConsumer<AgvTrafficManagementStatusReceived>
{
    private static readonly TimeSpan _minimumTickInterval = TimeSpan.FromTicks(TrafficManagementTickPublisher.TickInterval.Ticks * 4 / 5);

    public async Task Consume(ConsumeContext<StartRouteToNode> context)
    {
        var message = context.Message;

        if (!agvProvider.TryGetAgv(message.AgvId, out var agv))
        {
            await context.RespondAsync(new Rejected($"AGV '{message.AgvId}' not found"));
            return;
        }

        var targetNode = graphProvider.GetNodeByShortName(message.NodeShortName);

        if (targetNode is null)
        {
            await context.RespondAsync(new Rejected($"Target node '{message.NodeShortName}' not found"));
            return;
        }

        var routeRequest = new RouteRequest(agv, targetNode.RoutingNodes, Guid.Empty);

        routeManager.InvokeRoute(routeRequest);

        await context.RespondAsync(new Success());
    }

    public async Task Consume(ConsumeContext<StartRouteWithPick> context)
    {
        var message = context.Message;

        if (!agvProvider.TryGetAgv(message.AgvId, out var agv))
        {
            await context.RespondAsync(new Rejected($"AGV '{message.AgvId}' not found"));
            return;
        }

        var targetNode = graphProvider.GetNodeByShortName(message.NodeShortName);

        if (targetNode is null)
        {
            await context.RespondAsync(new Rejected($"Target node '{message.NodeShortName}' not found"));
            return;
        }

        List<RoutingNode> targetNodeCandidates = [.. targetNode.RoutingNodes.Where(rn => rn.LoadState.IsLoaded)];
        List<RoutingEdge> whitelistedActionEdges = [.. targetNode.RoutingEdges.Where(re => !re.StartNode.LoadState.IsLoaded && re.EndNode.LoadState.IsLoaded)];

        var routeRequest = new RouteRequest(agv, targetNodeCandidates, Guid.Empty)
        {
            WhitelistedActionEdges = whitelistedActionEdges
        };

        routeManager.InvokeRoute(routeRequest);

        await context.RespondAsync(new Success());
    }

    public async Task Consume(ConsumeContext<StartRouteWithDrop> context)
    {
        var message = context.Message;

        if (!agvProvider.TryGetAgv(message.AgvId, out var agv))
        {
            await context.RespondAsync(new Rejected($"AGV '{message.AgvId}' not found"));
            return;
        }

        var targetNode = graphProvider.GetNodeByShortName(message.NodeShortName);

        if (targetNode is null)
        {
            await context.RespondAsync(new Rejected($"Target node '{message.NodeShortName}' not found"));
            return;
        }

        List<RoutingNode> targetNodeCandidates = [.. targetNode.RoutingNodes.Where(rn => !rn.LoadState.IsLoaded)];
        List<RoutingEdge> whitelistedActionEdges = [.. targetNode.RoutingEdges.Where(re => re.StartNode.LoadState.IsLoaded && !re.EndNode.LoadState.IsLoaded)];

        var routeRequest = new RouteRequest(agv, targetNodeCandidates, Guid.Empty)
        {
            WhitelistedActionEdges = whitelistedActionEdges
        };

        routeManager.InvokeRoute(routeRequest);

        await context.RespondAsync(new Success());
    }

    public async Task Consume(ConsumeContext<AbortRoute> context)
    {
        bool wasAborted = routeManager.AbortRoute(context.Message.AgvId, Guid.Empty);

        if (wasAborted)
        {
            await context.RespondAsync(new Success());
        }
        else
        {
            await context.RespondAsync(new Rejected($"No route found for AGV '{context.Message.AgvId} found'"));
        }
    }

    public async Task Consume(ConsumeContext<GetRoute> context)
    {
        var agvId = context.Message.AgvId;

        var route = routeManager.GetRoute(agvId);

        if (route is null)
        {
            await context.RespondAsync(new Rejected($"No route found for AGV '{agvId}' found"));
            return;
        }

        List<string> routeStepInfos = [.. route.Steps.Select(s => s.ToString())];

        await context.RespondAsync(new GotRoute(agvId, route.Agv.CurrentRoutingNode?.ToString() ?? string.Empty, routeStepInfos));
    }

    public Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var message = context.Message;

        if (!agvProvider.TryGetAgv(message.CorrelationId, out var agv))
        {
            logger.LogError("Could not find agv information for {correlationId}", message.CorrelationId);
            return Task.CompletedTask;
        }

        if (agv is null)
        {
            throw new ArgumentException($"AGV '{message.CorrelationId}' not found");
        }

        var routeRequest = GetRouteRequestFromAgvAction(message.Action, agv);

        routeManager.InvokeRoute(routeRequest);

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<UpdateActionCommand> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<ActionAborted> context)
    {
        var message = context.Message;

        if (message.IsTemporary)
        {
            //todo: routeManager.PauseRoute(); When to resume?
        }

        routeManager.AbortRoute(message.CorrelationId, message.Action.CorrelationId);

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<RouteStepsProcessed> context)
    {
        var message = context.Message;

        routeManager.ProcessRouteSteps(message);

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<TrafficManagementTick> context)
    {
        if (!tickState.TryMarkProcessed(_minimumTickInterval))
        {
            return Task.CompletedTask; // discarded: a tick was already processed too recently
        }

        //logger.LogInformation("Processing traffic management tick.");

        routeManager.ExecuteRoutes();

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<GraphSegmentAdded> context)
    {
        graphProvider.AddGraphSegment(context.Message.GraphSegment);
        return Task.CompletedTask;
    }
    
    private RouteRequest GetRouteRequestFromAgvAction(AgvAction agvAction, RoutingAgv agv)
    {
        // mapping from GraphNode to new Node should not be necessary later
        string? targetNodeName = agvAction.Target.Node?.Name;

        if (targetNodeName is null)
        {
            throw new InvalidOperationException("Target node name is null.");
        }

        var targetNode = graphProvider.GetNodeByShortName(targetNodeName);

        if (targetNode is null)
        {
            throw new InvalidOperationException("Target node not found.");
        }

        var targetNodeCandidates = new List<RoutingNode>();
        var whitelistedActionEdges = new List<RoutingEdge>();

        switch (agvAction.ActionType)
        {
            case ActionType.Pick:
                targetNodeCandidates.AddRange(targetNode.RoutingNodes.Where(rn => rn.LoadState.IsLoaded));
                whitelistedActionEdges.AddRange(targetNode.RoutingEdges.Where(re => !re.StartNode.LoadState.IsLoaded && re.EndNode.LoadState.IsLoaded));
                break;

            case ActionType.Drop:
                targetNodeCandidates.AddRange(targetNode.RoutingNodes.Where(rn => !rn.LoadState.IsLoaded));
                whitelistedActionEdges.AddRange(targetNode.RoutingEdges.Where(re => re.StartNode.LoadState.IsLoaded && !re.EndNode.LoadState.IsLoaded));
                break;

            case ActionType.Move:
            case ActionType.Charge:
                targetNodeCandidates.AddRange(targetNode.RoutingNodes);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        var routeRequest = new RouteRequest(agv, targetNodeCandidates, agvAction.CorrelationId)
        {
            WhitelistedActionEdges = whitelistedActionEdges
        };

        return routeRequest;
    }

    public async Task Consume(ConsumeContext<AgvTypeAdded> context) //todo: handle update and remove
    {
        await graphProvider.AddOrUpdateAgvTypesAsync(context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<AgvDiscovered> context)
    {
        await agvProvider.InitializeAsync(context.CancellationToken);
    } 
    
    public async Task Consume(ConsumeContext<AgvAgvTypeUpdated> context)
    {
        await agvProvider.InitializeAsync(context.CancellationToken);
    }

    public Task Consume(ConsumeContext<AgvTrafficManagementStatusReceived> context)
    {
        agvProvider.UpdateAgvStatus(context.Message);

        return Task.CompletedTask;
    }
}