using FluentValidation;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Validators;

/// <summary>
/// Responsible for the validation of an action group from a list of conditions to determine if it is valid.
/// </summary>
public class ActionGroupValidator : AbstractValidator<ActionGroup>
{
    private const string PropertyMustNotBeNullOrEmpty = "{PropertyName} must not be null or empty.";
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
            .WithMessage(PropertyMustNotBeNullOrEmpty);

        RuleFor(p => p.CorrelationId)
            .NotEmpty()
            .WithMessage(PropertyMustNotBeNullOrEmpty);

        RuleForEach(p => p.Actions)
            .SetValidator(actionValidator);

        RuleFor(p => p.State)
            .Equal(ActionGroupState.Created)
            .WithMessage("{PropertyName} must be in the {ComparisonValue} state.");

        RuleFor(p => p)
            .Cascade(CascadeMode.Continue)
            .Must(p => p.Actions.All(a => a.ParentId == p.CorrelationId)).WithMessage("ParentId of each Action must match CorrelationId of ActionGroup.")
            .Must(ActionSequenceIdsMustBeSet).WithMessage("Action sequence Ids must be set.")
            .Must(ActionSequenceIdsMustBeUnique).WithMessage("Action sequence Ids must be unique.")
            .Must(ActionSequenceIdsMustNotHaveAGap).WithMessage("Action sequence must not have gaps.")
            .Must(ActionSequenceOrderedAscending).WithMessage("Actions within the group must be ordered by sequence.")
            .MustAsync(async (actionGroup, cancellationToken) => await CanHandleRouteAsync(actionGroup, cancellationToken)).WithMessage("There must be at least one Agv that knows the complete route of all actions within the given ActionGroup.")
            .DependentRules(() =>
            {
                When(_ => !_hasRoute, () =>
                {
                    //only check if nodes exist in case that there is no route, otherwise it's confirmed to be existing already
                    RuleForEach(p => p.Actions).ChildRules(action =>
                    {
                        action.RuleFor(a => a.TargetNodeId)
                            .MustAsync(async (nodeId, _) => await TargetNodeExists(nodeId))
                            .WithMessage("{PropertyName} ({PropertyValue}) must exist in the repository.");
                    });
                });
            });

        RuleFor(p => p.Actions)
            .NotNull()
            .NotEmpty()
            .WithMessage(PropertyMustNotBeNullOrEmpty);

        When(p => p.IsManual, () =>
        {
            RuleFor(p => p.Actions)
                .Must(actions => actions.Count == 1)
                .WithMessage("There can be no more than 1 {PropertyName} for an action group that is triggered by a manual action.");

            RuleFor(p => p.AssignedAgv)
                .NotNull()
                .NotEmpty()
                .WithMessage("{PropertyName} must not be null or empty if the action group was triggered manually.");
        });

        When(p => p.AssignedAgv != null, () =>
        {
            RuleFor(p => p.IsManual)
                .Must(p => p)
                .WithMessage("{PropertyName} must be true if an Agv is assigned to the action group.");

            RuleFor(x => x.AssignedAgv)
                .Must(_ => AgvExists())
                .WithMessage("{PropertyName} must exist in the repository.");
        });

        When(p => p.Actions.Any(a => a.ActionType is ActionType.Pick or ActionType.Drop), () =>
        {
            RuleFor(p => p.Actions)
                .Must(CheckPickDropSequence)
                .WithMessage("Pick and drop actions must match.");
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
        IOrderedEnumerable<AgvAction> orderedActions = actionGroup.Actions.OrderBy(a => a.Sequence);
        return orderedActions.SequenceEqual(actionGroup.Actions);
    }

    /// <summary>
    /// Checks if Agv or at least one Agv that knows the complete route of all actions within the given ActionGroup.
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<bool> CanHandleRouteAsync(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
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
            ICollection<Models.Agv.Agv> agvs = await _agvRepository.GetAllAgvsAsync();
            _hasRoute = await _routeService.HasAnyAgvRouteAsync(actionGroup, agvs, cancellationToken);
        }

        return _hasRoute;
    }

    private static bool CheckPickDropSequence(IReadOnlyCollection<AgvAction> actions)
    {
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

    private Task<bool> TargetNodeExists(string nodeId)
    {
        return _graphRepository.NodeExistsAsync(nodeId);
    }
}