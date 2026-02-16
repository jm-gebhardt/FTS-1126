using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TestPlanRunner.Domain;
using Gebhardt.Acc.TestPlanRunner.Providers;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using AgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Implementation of <see cref="ITestPlanManager"/>.
/// </summary>
public class TestPlanManager : ITestPlanManager
{
    private readonly ILogger<TestPlanManager> _logger;
    private readonly IAgvExecutionStateProvider _executionStateProvider;
    private readonly IActionCancellationEvaluator _cancellationEvaluator;

    private readonly ITestPlanDictionaryProvider _testPlanProvider;

    private readonly IKeyedSemaphoreProvider<IAgvIdleHandlerSemaphore> _agvIdleHandlerSemaphoreProvider;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IBus _bus;

    /// <inheritdoc cref="TestPlanManager"/>
    public TestPlanManager(
        ILogger<TestPlanManager> logger,
        IAgvExecutionStateProvider executionStateProvider,
        IActionCancellationEvaluator cancellationEvaluator,
        ITestPlanDictionaryProvider testPlanProvider,
        IKeyedSemaphoreProvider<IAgvIdleHandlerSemaphore> agvIdleHandlerSemaphoreProvider,
        IAgvRepository agvRepository,
        IActionGroupRepository actionGroupRepository,
        IBus bus)
    {
        _logger = logger;
        _executionStateProvider = executionStateProvider;
        _cancellationEvaluator = cancellationEvaluator;
        _testPlanProvider = testPlanProvider;
        _agvIdleHandlerSemaphoreProvider = agvIdleHandlerSemaphoreProvider;
        _agvRepository = agvRepository;
        _actionGroupRepository = actionGroupRepository;
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task ApplyTestPlanAsync(TestPlan testPlan)
    {
        _logger.LogInformation("Applying test plan: {TestPlanName}", testPlan.Name);
        await _bus.Publish(new StationConfigurationUpdateRequested());

        _testPlanProvider.ConcurrentDict.AddOrUpdate(testPlan.Name, testPlan, (_, _) => testPlan);

        IEnumerable<Guid> affectedAgvIds = await GetAffectedAgvIdsAsync(testPlan);

        foreach (var agvId in affectedAgvIds)
        {
            await ApplyTestPlanToAgvAsync(testPlan, agvId);
        }
    }

    private async Task ApplyTestPlanToAgvAsync(TestPlan testPlan, Guid agvCorrelationId)
    {
        var state = _executionStateProvider.GetOrCreate(agvCorrelationId);

        if (state.IsIdle || state.ActiveTestPlan == null)
        {
            _logger.LogDebug("AGV {AgvCorrelationId} is idle, starting test plan {TestPlanName} immediately",
                agvCorrelationId, testPlan.Name);

            state.Reset(testPlan);
            await QueueNextActionAsync(agvCorrelationId);
        }
        else
        {
            // AGV is busy, check if we can cancel
            if (state.CurrentActionType.HasValue)
            {
                bool canCancel = await _cancellationEvaluator.CanCancelAsync(agvCorrelationId, state.CurrentActionType.Value);

                if (canCancel)
                {
                    _logger.LogInformation(
                        "AGV {AgvCorrelationId} is executing cancellable action {ActionType}, cancelling and starting test plan {TestPlanName}",
                        agvCorrelationId, state.CurrentActionType.Value, testPlan.Name);

                    // Cancel current action - the AGV will become idle after the abort
                    if (state.CurrentActionGroupCorrelationId.HasValue)
                    {
                        await _bus.Publish(new AbortActionGroup(state.CurrentActionGroupCorrelationId.Value, AbortReason.TestPlanChange));
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "AGV {AgvCorrelationId} is executing non-cancellable action {ActionType}, marking test plan {TestPlanName} as pending",
                        agvCorrelationId, state.CurrentActionType.Value, testPlan.Name);
                }
            }

            // Set pending test plan - will be picked up when AGV becomes idle
            state.PendingTestPlan = testPlan;
            state.CancellationPending = true;
        }
    }

    /// <inheritdoc />
    public async Task RemoveTestPlanAsync(string name)
    {
        _logger.LogInformation("Removing test plan: {TestPlanName}", name);

        if (!_testPlanProvider.ConcurrentDict.TryRemove(name, out _))
        {
            return;
        }

        // Mark affected AGVs for removal
        foreach (var state in _executionStateProvider.GetAll())
        {
            if (state.ActiveTestPlan?.Name == name)
            {
                state.MarkedForRemoval = true;
                state.CancellationPending = true;

                // If the AGV is executing a cancellable action, cancel it
                if (state is { CurrentActionType: not null, CurrentActionGroupCorrelationId: not null })
                {
                    bool canCancel = await _cancellationEvaluator.CanCancelAsync(state.AgvCorrelationId, state.CurrentActionType.Value);

                    if (canCancel)
                    {
                        _logger.LogInformation("AGV {AgvCorrelationId} test plan removed, cancelling current cancellable action", state.AgvCorrelationId);

                        await _bus.Publish(new AbortActionGroup(state.CurrentActionGroupCorrelationId.Value, AbortReason.TestPlanChange));
                    }
                    else
                    {
                        _logger.LogInformation(
                            "AGV {AgvCorrelationId} test plan removed, waiting for non-cancellable action to complete",
                            state.AgvCorrelationId);
                    }
                }
            }

            // Also clear pending test plans that match
            if (state.PendingTestPlan?.Name == name)
            {
                state.PendingTestPlan = null;
                state.CancellationPending = false;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<TestPlan> GetActiveTestPlans()
    {
        return _testPlanProvider.ConcurrentDict.Values.ToList();
    }

    /// <inheritdoc />
    public TestPlan? GetTestPlan(string name)
    {
        return _testPlanProvider.ConcurrentDict.GetValueOrDefault(name);
    }

    private async Task QueueNextActionAsync(Guid agvCorrelationId)
    {
        var state = _executionStateProvider.GetOrCreate(agvCorrelationId);

        var currentStep = state.GetCurrentStep();
        if (currentStep == null)
        {
            _logger.LogDebug("No current step for AGV {AgvCorrelationId}", agvCorrelationId);
            return;
        }

        // Check if we need to wait
        if (state is { PendingWaitTimeSeconds: > 0, LastActionCompletedTime: not null })
        {
            var elapsed = DateTime.UtcNow - state.LastActionCompletedTime.Value;
            var waitTime = TimeSpan.FromSeconds(state.PendingWaitTimeSeconds);

            if (elapsed < waitTime)
            {
                var remainingWait = waitTime - elapsed;
                _logger.LogDebug("AGV {AgvCorrelationId} waiting {RemainingWait} before next action", agvCorrelationId, remainingWait);

                // Schedule the action after the wait time
                _ = Task.Run(async () =>
                {
                    await Task.Delay(remainingWait);
                    await QueueNextActionAsync(agvCorrelationId);
                });
                return;
            }
        }

        state.PendingWaitTimeSeconds = 0;

        var correlationId = Guid.NewGuid();
        string orderId = $"TestPlan_{state.ActiveTestPlan?.Name}_{correlationId.ToString()[..6]}";

        var action = new AgvAction(
            correlationId: Guid.NewGuid(),
            actionType: currentStep.ActionType,
            targetNode: currentStep.TargetNode.ToGraphNode(),
            parentId: correlationId)
        {
            Sequence = 0
        };

        var actionGroup = new ActionGroup(
            correlationId: correlationId,
            orderId: orderId,
            actions: [action],
            creator: ActionGroupCreator.TestPlanRunner,
            assignedAgv: agvCorrelationId);

        state.CurrentActionGroupCorrelationId = correlationId;
        state.CurrentActionType = currentStep.ActionType;
        state.IsIdle = false;

        _logger.LogInformation(
            "Queueing action for AGV {AgvCorrelationId}: TestPlan={TestPlanName}, Iteration={Iteration}/{Iterations}, Step={StepIndex}, Action={ActionType}, Target={TargetNode}",
            agvCorrelationId,
            state.ActiveTestPlan?.Name,
            state.CurrentIterationIndex + 1,
            state.ActiveTestPlan?.HasInfiniteIterations == false ? state.ActiveTestPlan?.Iterations : "∞",
            state.CurrentStepIndex,
            currentStep.ActionType,
            currentStep.TargetNode);

        await _bus.Publish(new QueueActionGroup(actionGroup, ThrowOnInvalid: false));
    }

    /// <inheritdoc />
    public async Task TryStartAgvAsync(Guid agvCorrelationId)
    {
        // Prevent concurrent execution when multiple idle events arrive
        await _agvIdleHandlerSemaphoreProvider.Get(agvCorrelationId).ExecuteLockedAsync(async () =>
        {
            _logger.LogDebug("Try starting AGV {AgvCorrelationId}", agvCorrelationId);
            bool hasUnstartedActions = await _actionGroupRepository.HasNotStartedActionsAsync(agvCorrelationId);
            if (hasUnstartedActions)
            {
                List<TransportOrder.Domain.Models.ActionGroup.ActionGroup> notStartedActions = await _actionGroupRepository.GetNotStartedActionsAsync(agvCorrelationId);
                _logger.LogDebug("AGV {AgvCorrelationId} has pending action groups, returning", agvCorrelationId);
                foreach (var actionGroup in notStartedActions)
                {
                    _logger.LogDebug("AGV {AgvCorrelationId} has pending action: ActionType={ActionType}, ActionGroupId={ActionGroupId}",
                        agvCorrelationId, actionGroup.Actions.First().ActionType, actionGroup.CorrelationId);
                }
                return;
            }

            var state = _executionStateProvider.GetOrCreate(agvCorrelationId);
            state.IsIdle = true;
            state.LastActionCompletedTime = DateTime.UtcNow;

            // Check if marked for removal
            if (state.MarkedForRemoval)
            {
                _logger.LogInformation("AGV {AgvCorrelationId} test plan execution stopped (file deleted)", agvCorrelationId);
                state.Reset(null);
                return;
            }

            // Check if there's a pending test plan to switch to
            if (state is { CancellationPending: true, PendingTestPlan: not null })
            {
                _logger.LogInformation("AGV {AgvCorrelationId} switching to pending test plan {TestPlanName}",
                    agvCorrelationId, state.PendingTestPlan.Name);

                state.SwitchToPendingTestPlan();
                await QueueNextActionAsync(agvCorrelationId);
                return;
            }

            // Check if we have an active test plan to continue
            if (state.ActiveTestPlan == null)
            {
                return;
            }

            // Get wait time from the completed step (before advancing)
            var completedStep = state.GetCurrentStep();
            if (completedStep != null)
            {
                state.PendingWaitTimeSeconds = completedStep.WaitTimeSeconds;
            }

            // Advance to next step
            if (!state.AdvanceToNextStep())
            {
                _logger.LogInformation("AGV {AgvCorrelationId} completed test plan {TestPlanName}",
                    agvCorrelationId, state.ActiveTestPlan.Name);

                state.Reset(null);
                return;
            }

            await QueueNextActionAsync(agvCorrelationId);
        });
    }

    private async Task<IEnumerable<Guid>> GetAffectedAgvIdsAsync(TestPlan testPlan)
    {
        if (!testPlan.AppliesToAllAgvs)
        {
            return testPlan.AgvCorrelationIds!;
        }

        ICollection<Agv> allAgvs = await _agvRepository.GetAllAgvsAsync();
        return allAgvs.Select(a => a.CorrelationId);
    }
}