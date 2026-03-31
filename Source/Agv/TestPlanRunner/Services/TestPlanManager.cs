using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Domain.Providers;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using MassTransit;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using AgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Implementation of <see cref="ITestPlanManager"/>.
/// </summary>
public class TestPlanManager : ITestPlanManager
{
    private const string TestPlanOrderIdPrefix = "TestPlan";

    private readonly ILogger<TestPlanManager> _logger;
    private readonly IAgvExecutionStateProvider _executionStateProvider;
    private readonly ITestStepCancellationEvaluator _cancellationEvaluator;

    private readonly ITestPlanDictionaryProvider _testPlanProvider;

    private readonly IKeyedSemaphoreProvider<IAgvIdleHandlerSemaphore> _agvIdleHandlerSemaphoreProvider;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IBus _bus;

    /// <inheritdoc cref="TestPlanManager"/>
    public TestPlanManager(
        ILogger<TestPlanManager> logger,
        IAgvExecutionStateProvider executionStateProvider,
        ITestStepCancellationEvaluator cancellationEvaluator,
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
        // Quick & dirty: give a bit time until configuration is reloaded
        await Task.Delay(1000);

        _testPlanProvider.ConcurrentDict.AddOrUpdate(testPlan.Name, testPlan, (_, _) => testPlan);

        IEnumerable<Guid> affectedAgvIds = await GetAffectedAgvIdsAsync(testPlan);

        foreach (var agvId in affectedAgvIds)
        {
            await ApplyTestPlanToAgvAsync(testPlan, agvId);
        }
    }

    private async Task ApplyTestPlanToAgvAsync(TestPlan testPlan, Guid agvCorrelationId)
    {
        await _agvIdleHandlerSemaphoreProvider.Get(agvCorrelationId).ExecuteLockedAsync(async () =>
        {
            var state = _executionStateProvider.GetOrCreate(agvCorrelationId);

        if (state.IsIdle || state.ActiveTestPlan == null)
        {
            _logger.LogDebug("AGV {AgvCorrelationId} is idle, starting test plan {TestPlanName} immediately",
                agvCorrelationId, testPlan.Name);

            state.Reset(testPlan);
            if (state.CurrentStepCorrelationId.HasValue)
            {
                _logger.LogDebug("AGV {AgvCorrelationId} current step already queued, skipping", agvCorrelationId);
                return;
            }
            await QueueNextStepAsync(agvCorrelationId);
        }
        else
        {
            // AGV is busy, check if we can cancel
            if (state.CurrentStepType.HasValue)
            {
                bool canCancel = await _cancellationEvaluator.CanCancelAsync(agvCorrelationId, state.CurrentStepType.Value);

                if (canCancel)
                {
                    _logger.LogInformation(
                        "AGV {AgvCorrelationId} is executing cancellable action {StepType}, cancelling and starting test plan {TestPlanName}",
                        agvCorrelationId, state.CurrentStepType.Value, testPlan.Name);

                    if (state.CurrentStepCorrelationId.HasValue)
                    {
                        await _bus.Publish(new AbortActionGroup(state.CurrentStepCorrelationId.Value, AbortReason.TestPlanChange));
                    }
                    
                    // We are ok with directly queueing the next test step after sending the abort.
                    state.IsStepCompleted = true;
                }
                else
                {
                    _logger.LogInformation(
                        "AGV {AgvCorrelationId} is executing non-cancellable action {StepType}, marking test plan {TestPlanName} as pending",
                        agvCorrelationId, state.CurrentStepType.Value, testPlan.Name);
                }
            }

            state.PendingTestPlan = testPlan;
            state.CancellationPending = true;
        }
        });
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
                if (state is { CurrentStepType: not null, CurrentStepCorrelationId: not null })
                {
                    bool canCancel = await _cancellationEvaluator.CanCancelAsync(state.AgvCorrelationId, state.CurrentStepType.Value);

                    if (canCancel)
                    {
                        _logger.LogInformation("AGV {AgvCorrelationId} test plan removed, cancelling current cancellable action", state.AgvCorrelationId);

                        await _bus.Publish(new AbortActionGroup(state.CurrentStepCorrelationId.Value, AbortReason.TestPlanChange));
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

    private async Task QueueNextStepAsync(Guid agvCorrelationId)
    {
        var state = _executionStateProvider.GetOrCreate(agvCorrelationId);

        var currentStep = state.GetCurrentStep();
        if (currentStep == null)
        {
            _logger.LogDebug("No current step for AGV {AgvCorrelationId}", agvCorrelationId);
            return;
        }

        // Check if we need to wait
        if (state is { PendingWaitTimeSeconds: > 0, LastStepCompletedTime: not null })
        {
            var elapsed = DateTime.UtcNow - state.LastStepCompletedTime.Value;
            var waitTime = TimeSpan.FromSeconds(state.PendingWaitTimeSeconds);

            if (elapsed < waitTime)
            {
                var remainingWait = waitTime - elapsed;
                _logger.LogDebug("AGV {AgvCorrelationId} waiting {RemainingWait} before next action", agvCorrelationId, remainingWait);

                // Schedule the action after the wait time
                _ = Task.Run(async () =>
                {
                    await Task.Delay(remainingWait);
                    await QueueNextStepAsync(agvCorrelationId);
                });
                return;
            }
        }

        state.PendingWaitTimeSeconds = 0;

        if (currentStep.StepType == StepType.HostOrder)
        {
            await QueueHostOrderAsync(agvCorrelationId, state, currentStep);
        }
        else
        {
            await QueueActionAsync(agvCorrelationId, state, currentStep);
        }
        
        _logger.LogInformation(
            "Queued {StepType} for AGV {AgvCorrelationId}: TestPlan={TestPlanName}, Step={StepNumber}, Iteration={Iteration}/{Iterations}, ActionGroupId={ActionGroupId}, Target={TargetNode}, Source={Source}, Destination={Destination}",
            currentStep.StepType,
            agvCorrelationId,
            state.ActiveTestPlan?.Name,
            state.CurrentStepIndex + 1,
            state.CurrentIterationIndex + 1,
            state.ActiveTestPlan?.HasInfiniteIterations == false ? state.ActiveTestPlan?.Iterations : "∞",
            state.CurrentStepCorrelationId,
            currentStep.TargetNode,
            currentStep.Source,
            currentStep.Destination);
    }

    private async Task QueueHostOrderAsync(Guid agvCorrelationId, AgvExecutionState state, TestPlanStep currentStep)
    {
        var correlationId = Guid.NewGuid();
        string orderId = $"{TestPlanOrderIdPrefix}_{state.ActiveTestPlan?.Name}_{correlationId.ToString()[..6]}";
        string hu = $"{correlationId.ToString()[..8]}";

        var orderDetails = new OrderDetails(
            correlationId: correlationId,
            orderId: orderId,
            source: currentStep.Source!,
            destination: currentStep.Destination!,
            leNo: hu,
            creator: HostOrderCreator.TestPlanRunner,
            leRotation: currentStep.LeRotation ?? 0);

        state.CurrentStepCorrelationId  = correlationId;
        state.CurrentStepType = StepType.HostOrder;
        state.IsIdle = false;

        await _bus.Publish(new HostOrderCreated(hu, orderDetails, agvCorrelationId));
    }

    private async Task QueueActionAsync(Guid agvCorrelationId, AgvExecutionState state, TestPlanStep currentStep)
    {
        var correlationId = Guid.NewGuid();
        string orderId = $"{TestPlanOrderIdPrefix}_{state.ActiveTestPlan?.Name}_{correlationId.ToString()[..6]}";

        var action = new AgvAction(
            correlationId: Guid.NewGuid(),
            actionType: StepTypeToActionType(currentStep.StepType),
            targetNode: currentStep.TargetNode!.ToGraphNode(),
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

        state.CurrentStepCorrelationId  = null;
        state.CurrentStepType = currentStep.StepType;
        state.IsIdle = false;

        await _bus.Publish(new QueueActionGroup(actionGroup, ThrowOnInvalid: false));
    }

    private static ActionType StepTypeToActionType(StepType stepType)
    {
        return stepType switch
        {
            StepType.Move => ActionType.Move,
            StepType.Pick => ActionType.Pick,
            StepType.Drop => ActionType.Drop,
            StepType.Charge => ActionType.Charge,
            _ => throw new ArgumentOutOfRangeException(nameof(stepType), stepType, "Cannot convert to ActionType")
        };
    }

    /// <inheritdoc />
    public async Task TryStartAgvAsync(Guid agvCorrelationId)
    {
        // Prevent concurrent execution when multiple idle events arrive
        await _agvIdleHandlerSemaphoreProvider.Get(agvCorrelationId).ExecuteLockedAsync(async () =>
        {
            _logger.LogDebug("Try starting AGV {AgvCorrelationId}", agvCorrelationId);
            var state = _executionStateProvider.GetOrCreate(agvCorrelationId);

            if (state is { CurrentStepCorrelationId: not null, IsStepCompleted: false })
            {
                _logger.LogDebug("AGV {AgvCorrelationId} is waiting for ActionGroupCompleted for step {StepCorrelationId}, skipping",
                    agvCorrelationId, state.CurrentStepCorrelationId);
                return;
            }

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

            if (state is { IsIdle: false, CurrentStepCorrelationId: not null })
            {
                var currentActionGroup = await _actionGroupRepository.GetActionGroupAsync(state.CurrentStepCorrelationId.Value);
                if (currentActionGroup is { State: ActionGroupState.Started or ActionGroupState.Created })
                {
                    _logger.LogDebug("AGV {AgvCorrelationId} current action group {ActionGroupId} is still in state {State}, skipping",
                        agvCorrelationId, state.CurrentStepCorrelationId.Value, currentActionGroup.State);
                    return;
                }
            }

            state.IsIdle = true;
            state.LastStepCompletedTime = DateTime.UtcNow;

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
                if (state.CurrentStepCorrelationId.HasValue)
                {
                    _logger.LogDebug("AGV {AgvCorrelationId} current step already queued after plan switch, skipping", agvCorrelationId);
                    return;
                }
                await QueueNextStepAsync(agvCorrelationId);
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
                state.PendingWaitTimeSeconds = completedStep.WaitTimeSeconds ?? 0;
            }

            // Advance to next step
            if (!state.AdvanceToNextStep())
            {
                _logger.LogInformation("AGV {AgvCorrelationId} completed test plan {TestPlanName}",
                    agvCorrelationId, state.ActiveTestPlan.Name);

                state.Reset(null);
                return;
            }

            state.IsStepCompleted = false;
            await QueueNextStepAsync(agvCorrelationId);
        });
    }

    /// <inheritdoc />
    public async Task SetStepCompletedAsync(Guid agvCorrelationId)
    {
        await _agvIdleHandlerSemaphoreProvider.Get(agvCorrelationId).ExecuteLockedAsync(() =>
        {
            var state = _executionStateProvider.GetOrCreate(agvCorrelationId);
            state.IsStepCompleted = true;
            return Task.CompletedTask;
        });
        
        await TryStartAgvAsync(agvCorrelationId);
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