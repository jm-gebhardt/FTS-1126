using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
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

    private static void EnsureType<TExpectedType>(object message)
    {
        if (message is not TExpectedType)
        {
            throw new Exception();
        }
    }

    /// <inheritdoc />
    public async Task SetBusyAsync(IAgv agv, bool wakeFromCharging)
    {
        IRequestClient<SetAgvBusy> client = _clientFactory.CreateRequestClient<SetAgvBusy>();
        Response<SetAgvBusySucceeded, SetAgvBusyRejected> response = await client.GetResponse<SetAgvBusySucceeded, SetAgvBusyRejected>(new SetAgvBusy(agv.CorrelationId));
        EnsureType<SetAgvBusySucceeded>(response.Message);
    }

    /// <inheritdoc />
    public async Task<bool> IsAgvReadyForBusyAsync(IAgv agv, bool ignoreCharging = false)
    {
        IRequestClient<RequestCanAgvTransitionToBusy> client = _clientFactory.CreateRequestClient<RequestCanAgvTransitionToBusy>();
        Response<CanAgvTransitionToBusyResponse> response = await client.GetResponse<CanAgvTransitionToBusyResponse>(new RequestCanAgvTransitionToBusy(agv.CorrelationId, ignoreCharging));
        return response.Message.Availability;
    }

    /// <inheritdoc />
    public async Task SetIdleAsync(IAgv agv)
    {
        IRequestClient<SetAgvIdle> client = _clientFactory.CreateRequestClient<SetAgvIdle>();
        Response<SetAgvIdleSucceeded, SetAgvIdleRejected> response = await client.GetResponse<SetAgvIdleSucceeded, SetAgvIdleRejected>(new SetAgvIdle(agv.CorrelationId));
        EnsureType<SetAgvIdleSucceeded>(response.Message);
    }
}