using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <summary>
/// Unified service coordinating DP1 target selection and SLW waiting, ensuring proper priority
/// when both compete for the same station resource.
/// </summary>
[UsedImplicitly]
public class UnifiedTargetSelector(
    ILogger<UnifiedTargetSelector> logger,
    PriorityAwareTargetSelector dp1TargetSelector,
    PriorityAwareWaitingHandler slwWaitingHandler,
    ISemaphoreProvider<IWaitingHandlerSemaphore> waitingHandlerSemaphore,
    ISemaphoreProvider<ITargetSelectionSemaphore> targetSelectorSemaphore)
    : BackgroundService, ITargetSelector, IWaitingHandler
{
    /// <summary>
    /// The time to wait between two target selection attempts.
    /// </summary>
    [PublicAPI] public readonly TimeSpan WaitingTime = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc cref="BackgroundService"/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialize();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during unified target selection");
            }
            finally
            {
                await Task.Delay(WaitingTime, stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        await dp1TargetSelector.ProcessActions(ShouldSkipStationForDp1Action, stoppingToken);
        await slwWaitingHandler.ProcessActions(ShouldSkipSlwActionDueToLongLoop, stoppingToken);
    }

    private async Task Initialize()
    {
        await dp1TargetSelector.Initialize();
        await slwWaitingHandler.Initialize();
    }

    #region Coordination Logic
    /// <summary>
    /// Determines if a DP1 action should skip a particular station due to SLW priority
    /// </summary>
    private bool ShouldSkipStationForDp1Action(Guid stationId)
    {
        var shortLoopTarget = slwWaitingHandler.GetTargetStation().GetAwaiter().GetResult();

        if (stationId != shortLoopTarget)
        {
            return false;
        }

        return ShouldShortLoopActionGetPriority(stationId);
    }

    /// <summary>
    /// Determines if an SLW action should be skipped due to DP1 priority
    /// </summary>
    private async Task<bool> ShouldSkipSlwActionDueToLongLoop(Guid slwActionId)
    {
        var shortLoopTarget = await slwWaitingHandler.GetTargetStation();
        return await ShouldLongLoopActionGetPriority(shortLoopTarget, slwActionId);
    }

    private bool ShouldShortLoopActionGetPriority(Guid stationId)
    {
        if (slwWaitingHandler.IsEmpty)
        {
            return false;
        }

        // Find the DP1 action that would select this station
        IOrderedEnumerable<KeyValuePair<Guid, DateTime>> relevantDp1Actions = dp1TargetSelector.PendingActions
            .Where(kvp => !dp1TargetSelector.EvaluatedActions.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Value);

        foreach (KeyValuePair<Guid, DateTime> dp1Entry in relevantDp1Actions)
        {
            bool wouldSelectStation = dp1TargetSelector.WouldSelectStation(stationId).GetAwaiter().GetResult();
            if (!wouldSelectStation)
            {
                continue;
            }

            // Check if any non-evaluated short loop action entered earlier
            KeyValuePair<Guid, DateTime> earliestSlwAction = slwWaitingHandler.PendingActions
                .Where(kvp => !slwWaitingHandler.EvaluatedActions.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault();

            if (earliestSlwAction.Key == Guid.Empty)
            {
                return false;
            }

            bool slwHasPriority = earliestSlwAction.Value < dp1Entry.Value;
            if (slwHasPriority)
            {
                logger.LogInformation("Short loop action {SlwAction} (entered {SlwTime}) has priority over DP1 action {Dp1Action} (entered {Dp1Time})",
                    earliestSlwAction.Key, earliestSlwAction.Value, dp1Entry.Key, dp1Entry.Value);
            }
            return slwHasPriority;
        }

        return false;
    }

    private async Task<bool> ShouldLongLoopActionGetPriority(Guid shortLoopTarget, Guid slwActionId)
    {
        if (dp1TargetSelector.IsEmpty)
        {
            return false;
        }

        if (!slwWaitingHandler.PendingActions.TryGetValue(slwActionId, out var slwEntryTime))
        {
            return false;
        }

        // Check if any non-evaluated DP1 action would select the short loop target and entered earlier
        IOrderedEnumerable<KeyValuePair<Guid, DateTime>> relevantDp1Actions = dp1TargetSelector.PendingActions
            .Where(kvp => !dp1TargetSelector.EvaluatedActions.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Value);

        foreach (KeyValuePair<Guid, DateTime> dp1Entry in relevantDp1Actions)
        {
            bool targetWouldBeShortLoopTargetStation = await dp1TargetSelector.WouldSelectStation(shortLoopTarget);

            if (targetWouldBeShortLoopTargetStation && dp1Entry.Value < slwEntryTime)
            {
                logger.LogInformation("DP1 action {Dp1Action} (entered {Dp1Time}) has priority over SLW action {SlwAction} (entered {SlwTime})",
                    dp1Entry.Key, dp1Entry.Value, slwActionId, slwEntryTime);
                return true;
            }
        }

        return false;
    }
    #endregion

    #region ITargetSelector Implementation
    /// <inheritdoc cref="ITargetSelector"/>
    public void SetStationCount(int stationCount)
    {
        dp1TargetSelector.SetStationCount(stationCount);
    }

    /// <inheritdoc cref="ITargetSelector"/>
    public Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId)
    {
        return targetSelectorSemaphore.ExecuteLockedAsync(async () => { await dp1TargetSelector.Enqueue(actionCorrelationId, agvCorrelationId); });
    }

    /// <inheritdoc cref="ITargetSelector"/>
    public Task Dequeue(Guid actionCorrelationId)
    {
        return targetSelectorSemaphore.ExecuteLockedAsync(async () => { await dp1TargetSelector.Dequeue(actionCorrelationId); });
    }

    /// <inheritdoc cref="ITargetSelector"/>
    public void SetUseJamState(bool useJamState)
    {
        dp1TargetSelector.SetUseJamState(useJamState);
    }
    #endregion

    #region IWaitingHandler Implementation
    /// <inheritdoc cref="IWaitingHandler"/>
    public Task StartHandling(Guid actionCorrelationId, Guid agvCorrelationId, DateTime orderCreationTime)
    {
        return waitingHandlerSemaphore.ExecuteLockedAsync(async () => { await slwWaitingHandler.StartHandling(actionCorrelationId, agvCorrelationId, orderCreationTime); });
    }

    /// <inheritdoc cref="IWaitingHandler"/>
    public Task StopHandling(Guid actionCorrelationId)
    {
        return waitingHandlerSemaphore.ExecuteLockedAsync(async () => { await slwWaitingHandler.StopHandling(actionCorrelationId); });
    }
    #endregion
}