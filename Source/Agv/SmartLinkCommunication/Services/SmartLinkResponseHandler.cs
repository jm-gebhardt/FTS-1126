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
                        _logger.LogInformation("IO {IioId} reached target state {CurrentState} and stayed there for {After}, executing action now", actionConfig.Id, actionConfig.State, actionConfig.After);
                        _ioActionExecutionService.ExecuteActionAsync(actionConfig);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error executing action for IO {IoId}", actionConfig.Id);
                    }
                });
        }

        var ioStateTracker = _stateTrackerService.IoStateTrackers[actionConfig];
        bool previousState = ioStateTracker.CurrentState;
        ioStateTracker.CurrentState = state;
        if (previousState != state)
        {
            _logger.LogDebug("IO {IoId} changed from {PreviousState} to {CurrentState}", actionConfig.Id, previousState, state);

            if (state == actionConfig.State)
            {
                _logger.LogDebug("IO {IoId} reached target state {CurrentState}. If it stays here for {After}, the action will be executed", actionConfig.Id, state, actionConfig.After);
            }
        }
        else
        {
            if (ioStateTracker.IsTimerRunning)
            {
                _logger.LogDebug("IO {IoId} stayed {CurrentState}, but waiting time is not over yet", actionConfig.Id, state);
            }
            else
            {
                _logger.LogDebug("IO {IoId} has already executed the action", actionConfig.Id);
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
                    _logger.LogError("IO {IoId} not found in SmartLink response", id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing IO {IoId}", ioConfig.Id);
            }
        }

        return Task.CompletedTask;
    }
}