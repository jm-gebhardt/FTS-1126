using Gebhardt.Acc.SmartLinkCommunication.Models;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Track state of io pins and trigger IIoActionExecutionService when it was satisfied
/// </summary>
public class SmartLinkResponseHandler : ISmartLinkResponseHandler
{
    private readonly IIoActionExecutionService _ioActionExecutionService;
    private readonly IModuleConfigProvider _moduleConfigProvider;
    private readonly IIoStateTrackerService _stateTrackerService;
    private readonly ILogger<SmartLinkResponseHandler> _logger;

    /// <inheritdoc cref="SmartLinkResponseHandler" />
    /// <param name="ioActionExecutionService"></param>
    /// <param name="moduleConfigProvider"></param>
    /// <param name="stateTrackerService"></param>
    /// <param name="logger"></param>
    public SmartLinkResponseHandler(IIoActionExecutionService ioActionExecutionService, IModuleConfigProvider moduleConfigProvider, IIoStateTrackerService stateTrackerService, ILogger<SmartLinkResponseHandler> logger)
    {
        _ioActionExecutionService = ioActionExecutionService;
        _moduleConfigProvider = moduleConfigProvider;
        _stateTrackerService = stateTrackerService;
        _logger = logger;
    }

    private void ProcessIoState(IoActionConfig actionConfig, bool state)
    {
        if (!_stateTrackerService.IoStateTrackers.ContainsKey(actionConfig))
        {
            _stateTrackerService.IoStateTrackers[actionConfig] = new IoStateTracker()
                .SetupTimer(actionConfig.After, actionConfig.State, () =>
                {
                    try
                    {
                        _logger.LogInformation("IO {ioId} reached target state {currentState} and stayed there for {after}. Executing action now.", actionConfig.Id, actionConfig.State, actionConfig.After);
                        _ioActionExecutionService.ExecuteActionAsync(actionConfig);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error executing action for IO {ioId}", actionConfig.Id);
                    }
                });
        }

        var ioStateTracker = _stateTrackerService.IoStateTrackers[actionConfig];
        bool previousState = ioStateTracker.CurrentState;
        ioStateTracker.CurrentState = state;
        if (previousState != state)
        {
            _logger.LogDebug("IO {ioId} changed from {previousState} to {currentState}", actionConfig.Id, previousState, state);

            if (state == actionConfig.State)
            {
                _logger.LogDebug("IO {ioId} reached target state {currentState}. If it stays here for {after}, the action will be executed.", actionConfig.Id, state, actionConfig.After);
            }
        }
        else
        {
            if (ioStateTracker.IsTimerRunning)
            {
                _logger.LogDebug("IO {ioId} stayed {currentState}, but waiting time is not over yet.", actionConfig.Id, state);
            }
            else
            {
                _logger.LogDebug("IO {ioId} has already executed the action.", actionConfig.Id);
            }
        }
    }

    /// <inheritdoc />
    public Task HandleAsync(SmartLinkData data)
    {
        // only handle configured IOs
        foreach (var ioConfig in _moduleConfigProvider.ModuleConfig.IoActionConfigs)
        {
            try
            {
                string id = ioConfig.Id;
                if (data.TryGetValue(id, out var io))
                {
                    // as we have only configured input pins, we can safely cast to bool, as input pins have a State in all cases (whereas output do not)
                    ProcessIoState(ioConfig, (bool)io.State!);
                }
                else
                {
                    _logger.LogError("IO {ioId} not found in SmartLink response", id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing IO {ioId}", ioConfig.Id);
            }
        }

        return Task.CompletedTask;
    }
}