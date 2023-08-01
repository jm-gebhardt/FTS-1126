using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class AgvStateService : IAgvStateService
{
    private readonly IClientFactory _clientFactory;

    /// <summary>
    /// Ctor
    /// <param name="clientFactory"></param>
    /// </summary>
    public AgvStateService(IClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <inheritdoc />
    public async Task SetBusyAsync(IAgv agv, bool wakeFromCharging, bool ignoreLock, CancellationToken cancellationToken)
    {
        IRequestClient<SetAgvBusy> client = _clientFactory.CreateRequestClient<SetAgvBusy>();
        Response<SetAgvBusySucceeded, SetAgvBusyRejected> response = await client.GetResponse<SetAgvBusySucceeded, SetAgvBusyRejected>(new SetAgvBusy(agv.CorrelationId, ignoreLock), cancellationToken);
        if (response.Message is not SetAgvBusySucceeded)
        {
            throw new SetAgvBusyFailedException(agv.CorrelationId, response.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvReadyForBusyAsync(Guid agvCorrelationId, CancellationToken cancellationToken, bool ignoreCharging = false, bool ignoreLocked = false)
    {
        IRequestClient<CanAgvTransitionToBusyRequest> client = _clientFactory.CreateRequestClient<CanAgvTransitionToBusyRequest>();
        var canAgvTransitionToBusyRequest = new CanAgvTransitionToBusyRequest(agvCorrelationId, ignoreCharging, ignoreLocked);
        Response<CanAgvTransitionToBusyResponse> response = await client.GetResponse<CanAgvTransitionToBusyResponse>(canAgvTransitionToBusyRequest, cancellationToken);
        return response.Message.Availability;
    }

    /// <inheritdoc />
    public async Task SetIdleAsync(Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        IRequestClient<SetAgvIdle> client = _clientFactory.CreateRequestClient<SetAgvIdle>();
        Response<SetAgvIdleSucceeded, SetAgvIdleRejected> response = await client.GetResponse<SetAgvIdleSucceeded, SetAgvIdleRejected>(new SetAgvIdle(agvCorrelationId), cancellationToken);
        if (response.Message is not SetAgvIdleSucceeded)
        {
            throw new SetAgvIdleFailedException(agvCorrelationId, response.Message);
        }
    }
}