using FluentValidation;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Domain.Validators;

/// <summary>
/// Responsible for the validation of an action group from a list of conditions to determine if it is valid.
/// </summary>
public class ActionGroupValidator : AbstractValidator<ActionGroup>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IRouteService _routeService;
    private readonly IGraphRepository _graphRepository;
    private Models.Agv.Agv? _agv;
    private bool _hasRoute;

    /// <inheritdoc cref="ActionGroupValidator" />
    public ActionGroupValidator(IAgvRepository agvRepository,
        IRouteService routeService,
        IValidator<AgvAction> actionValidator,
        IGraphRepository graphRepository
    )
    {
        _agvRepository = agvRepository;
        _routeService = routeService;
        _graphRepository = graphRepository;
        _hasRoute = false;

        RuleFor(p => p.OrderId)
            .NotNull()
            .NotEmpty()
            .WithReason(Reasons.OrderIdIsNullOrEmpty);

        RuleFor(p => p.CorrelationId)
            .NotEmpty()
            .WithReason(Reasons.CorrelationIdIsNullOrEmpty);

        RuleForEach(p => p.Actions)
            .SetValidator(actionValidator);

        RuleFor(p => p.State)
            .Equal(ActionGroupState.Created)
            .WithReason(Reasons.ActionGroupStateMustBeCreated, p => p.State);

        RuleFor(p => p)
            .Cascade(CascadeMode.Continue)
            .Must(p => p.Actions.All(a => a.ParentId == p.CorrelationId)).WithReason(Reasons.ParentIdOfEachActionMustMatchCorrelationIdOfActionGroup)
            .Must(ActionSequenceIdsMustBeSet).WithReason(Reasons.ActionSequenceIdsMustBeSet)
            .Must(ActionSequenceIdsMustBeUnique).WithReason(Reasons.ActionSequenceIdsMustBeUnique)
            .Must(ActionSequenceIdsMustNotHaveAGap).WithReason(Reasons.ActionSequenceMustNotHaveGaps)
            .Must(ActionSequenceOrderedAscending).WithReason(Reasons.ActionsWithinTheGroupMustBeOrderedBySequence)
            .MustAsync(async (actionGroup, cancellationToken) => await CanHandleRouteAsync(actionGroup, cancellationToken)).WithReason(HostOrderFailureReasons.ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup)
            .DependentRules(() =>
            {
                When(_ => !_hasRoute, () =>
                {
                    //only check if nodes exist in case that there is no route, otherwise it's confirmed to be existing already
                    RuleForEach(p => p.Actions).ChildRules(action =>
                    {
                        action.When(a => !a.IsMoveToCoordinate, () =>
                            action.RuleFor(a => a.Target.NodeId!.Value)
                                .MustAsync(async (nodeId, _) => await TargetNodeExists(nodeId))
                                .WithReason(a => a.ActionType switch
                                {
                                    ActionType.Pick => Reasons.PickStationNodeDoesNotExist,
                                    ActionType.Drop => Reasons.DropStationNodeDoesNotExist,
                                    _ => Reasons.TargetNodeDoesNotExist
                                }, a => a.Target.Node!.Name));
                    });
                });
            });

        RuleFor(p => p.Actions)
            .NotNull()
            .NotEmpty()
            .WithReason(Reasons.ActionsMustBeSet);

        When(p => p.IsManual && !p.IsMovingCargo, () =>
        {
            RuleFor(p => p.AssignedAgv)
                .NotNull()
                .NotEmpty()
                .WithReason(Reasons.ManualActionMustHaveAgvAssigned);
        });

        When(p => p.IsManual && p.Actions.Any(a => a.ActionType == ActionType.Pick) && p.Actions.Any(a => a.ActionType == ActionType.Drop), () =>
        {
            RuleFor(x => x.Actions)
                .Must(a =>
                {
                    var pick = a.First(ac => ac.ActionType == ActionType.Pick);
                    var drop = a.First(ac => ac.ActionType == ActionType.Drop);

                    return pick.Target != drop.Target;
                }).WithReason(Reasons.SourceTargetIdentical, a => a.Actions.First(ac => ac.ActionType == ActionType.Pick).Target.Node?.Name);
        });

        When(p => p.AssignedAgv != null, () =>
        {
            RuleFor(x => x.AssignedAgv)
                .Must(_ => AgvExists())
                .WithReason(Reasons.AgvDoesNotExist, x => x.AssignedAgv);
        });

        When(p => p.Actions.Any(a => a.ActionType is ActionType.Pick or ActionType.Drop), () =>
        {
            RuleFor(p => p)
                .Must(a => CheckPickDropSequence(a.Actions, a.Creator))
                .WithReason(Reasons.PickDropSequenceMustBeValid);
        });
    }

    /// <summary>
    /// Checks if the assigned Agv exists in the repository 
    /// </summary>
    /// <returns></returns>
    private bool AgvExists()
    {
        return _agv != null;
    }

    private static bool ActionSequenceIdsMustBeSet(ActionGroup actionGroup)
    {
        bool sequenceSet = actionGroup.Actions.Count <= 1 || actionGroup.Actions.Count(a => a.Sequence == 0) == 1;
        return sequenceSet;
    }

    private static bool ActionSequenceIdsMustBeUnique(ActionGroup actionGroup)
    {
        IEnumerable<int> duplicates = actionGroup.Actions.GroupBy(a => a.Sequence).Where(g => g.Count() > 1).Select(g => g.Key);
        bool isSequenceUnique = !duplicates.Any();
        return isSequenceUnique;
    }

    private static bool ActionSequenceIdsMustNotHaveAGap(ActionGroup actionGroup)
    {
        if (actionGroup is { Creator: ActionGroupCreator.TransportOrderWebApi, Actions.Count: 1 })
        {
            return true;
        }
        List<int> sequenceIds = actionGroup.Actions.Select(a => a.Sequence).OrderBy(s => s).ToList();
        bool isSequenceOrdered = sequenceIds.SequenceEqual(Enumerable.Range(0, sequenceIds.Count));
        return isSequenceOrdered;
    }

    /// <summary>
    /// Checks whether the order of actions within the group are ascending
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <returns></returns>
    private static bool ActionSequenceOrderedAscending(ActionGroup actionGroup)
    {
        if (actionGroup is { Creator: ActionGroupCreator.TransportOrderWebApi, Actions.Count: 1 })
        {
            return true;
        }
        IOrderedEnumerable<AgvAction> orderedActions = actionGroup.Actions.OrderBy(a => a.Sequence);
        return orderedActions.SequenceEqual(actionGroup.Actions);
    }

    /// <summary>
    /// Checks if Agv or at least one Agv that knows the complete route of all actions within the given ActionGroup.
    /// </summary>
    /// <param name="actionGroup">The action group.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    private async Task<bool> CanHandleRouteAsync(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        if (actionGroup.Actions.Any(a => a.IsMoveToCoordinate) && actionGroup.AssignedAgv != null)
        {
            _agv = await _agvRepository.GetAgvAsync(actionGroup.AssignedAgv!.Value);
            _hasRoute = true;
            return _hasRoute;
        }

        if (actionGroup.AssignedAgv != null)
        {
            _agv = await _agvRepository.GetAgvAsync(actionGroup.AssignedAgv!.Value);
            if (_agv == null)
            {
                return false;
            }

            double? costs = await _routeService.GetCostsForFirstActionAsync(_agv, actionGroup, null, cancellationToken);
            _hasRoute = costs != null;
        }
        else
        {
            ICollection<Models.Agv.Agv> agvs = await _agvRepository.GetAllAgvsWithGraphAsync();
            _hasRoute = await _routeService.HasAnyAgvRouteAsync(actionGroup, agvs, cancellationToken);
        }

        return _hasRoute;
    }

    private static bool CheckPickDropSequence(IReadOnlyCollection<AgvAction> actions, ActionGroupCreator creator)
    {
        if (actions.Count == 1 && creator is ActionGroupCreator.TransportOrderWebApi or ActionGroupCreator.TestPlanRunner)
        {
            return true;
        }
        int stack = 0;
        foreach (var action in actions)
        {
            stack += action.ActionType switch
            {
                ActionType.Pick => 1,
                ActionType.Drop => -1,
                _ => 0
            };

            if (stack < 0)
            {
                return false;
            }
        }
        return stack == 0;
    }

    private Task<bool> TargetNodeExists(Guid nodeId)
    {
        return _graphRepository.NodeExistsByIdAsync(nodeId);
    }
}