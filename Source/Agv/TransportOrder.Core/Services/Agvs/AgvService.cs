using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <inheritdoc />
public class AgvService : IAgvService
{
    private readonly IAgvRegistry _agvRegistry;
    private readonly IAgvStateService _agvStateService;
    private readonly ILogger<AgvService> _logger;

    /// <inheritdoc cref="AgvService"/>
    /// <param name="agvRegistry"></param>
    /// <param name="agvStateService"></param>
    /// <param name="logger"></param>
    public AgvService(IAgvRegistry agvRegistry, IAgvStateService agvStateService, ILogger<AgvService> logger)
    {
        _agvRegistry = agvRegistry;
        _agvStateService = agvStateService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAgv>> GetReadyAgvsAsync(bool ignoreChargingFlag)
    {
        IQueryable<IAgv> agvs = _agvRegistry.GetAllAgvs();
        return GetReadyAgvsAsync(agvs, ignoreChargingFlag);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAgv>> GetReadyAgvsAsync(IEnumerable<IAgv> agvs, bool ignoreChargingFlag)
    {
        Dictionary<Task<bool>, IAgv> readyTasksByAgv = agvs.ToDictionary(a => IsAgvReadyForBusyAsync(a, ignoreChargingFlag), a => a);

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
                .ForEach(e => _logger.LogError("Failed to execute task one of the IsAgvReady tasks: {Exception}", e));
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> IsAgvReadyForBusyAsync(IAgv agv, bool ignoreChargingFlag) => _agvStateService.IsAgvReadyForBusyAsync(agv, ignoreChargingFlag);
}