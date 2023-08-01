using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Queries;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class AgvService : IAgvService
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IAgvStateService _agvStateService;
    private readonly IClientFactory _clientFactory;
    private readonly ILogger<AgvService> _logger;

    /// <inheritdoc cref="AgvService"/>
    /// <param name="agvRegistry"></param>
    /// <param name="agvStateService"></param>
    /// <param name="clientFactory"></param>
    /// <param name="logger"></param>
    public AgvService(IAgvRegistry agvRegistry, IAgvStateService agvStateService, IClientFactory clientFactory, ILogger<AgvService> logger)
    {
        _agvRegistry = agvRegistry;
        _agvStateService = agvStateService;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IAgv> GetAgvAsync(Guid? agvCorrelationId, CancellationToken cancellationToken)
    {
        if (agvCorrelationId == null)
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }
        return (await _clientFactory.CreateRequestClient<GetAgvRequest>().GetResponse<GetAgvResult>(new GetAgvRequest((Guid) agvCorrelationId), cancellationToken)).Message.Agv;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAgv>> GetReadyAgvsAsync(bool ignoreChargingFlag, CancellationToken cancellationToken)
    {
        IQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs();

        return GetReadyAgvsAsync(agvs, ignoreChargingFlag, cancellationToken);
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

    /// <inheritdoc />
    public Task<bool> IsAgvReadyForBusyAsync(Guid agvCorrelationId, CancellationToken cancellationToken, bool ignoreCharging = false, bool ignoreLocked = false) => _agvStateService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: ignoreCharging, ignoreLocked: ignoreLocked);
}