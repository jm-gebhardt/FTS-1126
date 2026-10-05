using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using DomainRoutePlan = Gebhardt.Acc.TransportOrder.Domain.Models.Route.RoutePlan;
using MessageRoutePlan = Gebhardt.Acc.Common.Messages.ActionGroups.RoutePlan;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;

/// <inheritdoc />
[UsedImplicitly]
public class TrafficManager(
    IMapper mapper,
    IPublishEndpoint publishEndpoint,
    IAgvRepository agvRepository,
    ILogger<TrafficManager> logger) : ITrafficManager
{
    /// <inheritdoc />
    public async Task AddNewRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default)
    {
        var plan = DomainRoutePlan.FromAction(action);
        await ValidateFirstNodeAsync(agvCorrelationId, plan);
        await publishEndpoint.Publish(new DispatchRouteCommand(agvCorrelationId, serialNumber, mapper.Map<MessageRoutePlan>(plan), action), ct);
    }

    /// <inheritdoc />
    public async Task UpdateRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default)
    {
        var plan = DomainRoutePlan.FromAction(action);
        await ValidateFirstNodeAsync(agvCorrelationId, plan);
        await publishEndpoint.Publish(new UpdateDispatchedRouteCommand(agvCorrelationId, serialNumber, mapper.Map<MessageRoutePlan>(plan), action), ct);
    }

    /// <inheritdoc />
    public async Task AbortAndDispatchRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, Guid? abortedActionCorrelationId, CancellationToken ct = default)
    {
        var plan = DomainRoutePlan.FromAction(action);
        await ValidateFirstNodeAsync(agvCorrelationId, plan);
        await publishEndpoint.Publish(new AbortAndDispatchRouteCommand(agvCorrelationId, serialNumber, action, mapper.Map<MessageRoutePlan>(plan), abortedActionCorrelationId), ct);
    }

    /// <inheritdoc />
    public async Task ResendRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default)
    {
        var plan = DomainRoutePlan.FromAction(action);
        await ValidateFirstNodeAsync(agvCorrelationId, plan);
        await publishEndpoint.Publish(new ResendDispatchedRouteCommand(agvCorrelationId, serialNumber, mapper.Map<MessageRoutePlan>(plan), action), ct);
    }

    /// <inheritdoc />
    public Task HandleNewBaseRequestAsync(Guid agvCorrelationId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task ValidateFirstNodeAsync(Guid agvCorrelationId, DomainRoutePlan plan)
    {
        if (plan.BaseNodes.Count == 0)
        {
            logger.LogWarning("AGV {AgvCorrelationId} has no route; Last node requirement cannot be verified", agvCorrelationId);
            return;
        }

        var lastNodeId = await agvRepository.GetLastNodeIdAsync(agvCorrelationId);
        if (lastNodeId.GetValueOrDefault() == Guid.Empty)
        {
            logger.LogWarning("AGV {AgvCorrelationId} has no last node; Last node requirement cannot be verified", agvCorrelationId);
        }
        else if (lastNodeId != plan.BaseNodes[0].Id)
        {
            logger.LogWarning("AGV {AgvCorrelationId} last node {LastNodeId} does not match route's first node {FirstNodeId}", agvCorrelationId, lastNodeId, plan.BaseNodes[0].Id);
        }
    }
}