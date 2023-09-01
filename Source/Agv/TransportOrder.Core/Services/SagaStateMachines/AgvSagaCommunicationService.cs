using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;

/// <inheritdoc />
public class AgvSagaCommunicationService : IAgvSagaCommunicationService
{
    private readonly IClientFactory _clientFactory;
    private readonly IAgvRegistry _agvRegistry;
    private readonly ILogger<AgvSagaCommunicationService> _logger;

    /// <summary>
    /// Ctor
    /// <param name="clientFactory"></param>
    /// <param name="agvRegistry"></param>
    /// <param name="logger"></param>
    /// </summary>
    public AgvSagaCommunicationService(IClientFactory clientFactory, IAgvRegistry agvRegistry, ILogger<AgvSagaCommunicationService> logger)
    {
        _clientFactory = clientFactory;
        _agvRegistry = agvRegistry;
        _logger = logger;
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

    /// <inheritdoc />
    public async Task<IAgv> GetAgvAsync(Guid? agvCorrelationId, CancellationToken cancellationToken)
    {
        if (agvCorrelationId == null)
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }
        return (await _clientFactory.CreateRequestClient<GetAgvRequest>().GetResponse<GetAgvResult>(new GetAgvRequest((Guid)agvCorrelationId), cancellationToken)).Message.Agv;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAgv>> GetReadyAgvsAsync(bool ignoreChargingFlag, CancellationToken cancellationToken)
    {
        List<IAgv> agvs = await _agvRegistry.GetAllAgvsWithGraphAsync();

        return await GetReadyAgvsAsync(agvs, ignoreChargingFlag, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAgv>> GetReadyAgvsAsync(IEnumerable<IAgv> agvs, bool ignoreChargingFlag, CancellationToken cancellationToken)
    {
        Dictionary<Task<bool>, IAgv> readyTasksByAgv = agvs.ToDictionary(a => IsAgvReadyForBusyAsync(a.CorrelationId, cancellationToken, ignoreCharging: ignoreChargingFlag), a => a);

        Task<bool[]> tasks = Task.WhenAll(readyTasksByAgv.Select(r => r.Key));

        try
        {
            await tasks;
            // tasks are already completed, just just take result
            Dictionary<IAgv, bool> readyByAgv = readyTasksByAgv.ToDictionary(r => r.Value, r => r.Key.Result);
            List<IAgv> readyAgvs = readyByAgv.Where(a => a.Value).Select(a => a.Key).ToList();
            return readyAgvs;
        }
        catch
        {
            tasks.Exception?.InnerExceptions
                .ToList()
                .ForEach(e => _logger.LogError(e, "Failed to execute task one of the IsAgvReady tasks"));
            throw;
        }
    }
}