using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvRoute request
/// </summary>
[UsedImplicitly]
public class GetCurrentAgvRouteRequestHandler : IConsumer<GetCurrentAgvRoute>, IConsumer<GetCurrentAgvRoutes>
{
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public GetCurrentAgvRouteRequestHandler(IAgvRouteRetrievalService agvRouteRetrievalService, IAgvDb agvDb)
    {
        _agvRouteRetrievalService = agvRouteRetrievalService;
        _agvDb = agvDb;
    }

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetCurrentAgvRoute> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _agvDb.Agvs.ThrowIfAgvNotExistsAsync(correlationId, context.CancellationToken);

        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(correlationId, cancellationToken: context.CancellationToken);
        await context.RespondAsync(new GotCurrentAgvRoute(agvRoute));
    }

    /// <inheritdoc cref="GetCurrentAgvRouteRequestHandler"/>
    public async Task Consume(ConsumeContext<GetCurrentAgvRoutes> context)
    {
        ICollection<Guid> correlationIds = context.Message.CorrelationIds;
        await _agvDb.Agvs.ThrowIfAgvsNotExistAsync(correlationIds, context.CancellationToken);

        ICollection<CurrentAgvRouteReadModel> agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(correlationIds, cancellationToken: context.CancellationToken);
        await context.RespondAsync(new GotCurrentAgvRoutes(agvRoute));
    }
}