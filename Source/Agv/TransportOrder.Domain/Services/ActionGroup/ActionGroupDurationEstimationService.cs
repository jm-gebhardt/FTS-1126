using System.Numerics;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc/>
public class ActionGroupDurationEstimationService : IActionGroupDurationEstimationService
{
    private readonly IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)> _deviationCache;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly TimeSpan _updateSpan = TimeSpan.FromHours(1);
    private readonly IAgvPositionRepository _agvPositionRepository;
    private readonly IGraphRepository _graphRepository;

    /// <inheritdoc cref="ActionGroupDurationEstimationService"/>
    public ActionGroupDurationEstimationService(IConcurrentDictionaryProvider<string, (double, DateTime)> deviationCache, IActionGroupRepository actionGroupRepository, IAgvPositionRepository agvPositionRepository, IGraphRepository graphRepository)
    {
        _deviationCache = deviationCache;
        _actionGroupRepository = actionGroupRepository;
        _agvPositionRepository = agvPositionRepository;
        _graphRepository = graphRepository;
    }

    /// <summary>
    ///  Gets the average deviation between the actual and estimated
    /// duration [s] of an action group for the given target nodes.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns>The deviation in seconds.</returns>
    public async Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, string[] targetNodes)
    {
        string key = GetKey(agvCorrelationId, targetNodes);
        bool deviationExists = _deviationCache.ConcurrentDict.TryGetValue(key, out var deviation);
        var now = DateTime.UtcNow;
        if (!deviationExists || now - deviation.lastUpdated > _updateSpan)
        {
            deviation.value = await _actionGroupRepository.GetAverageDurationEstimateDeviationAsync(agvCorrelationId, targetNodes);
            deviation.lastUpdated = now;
            _deviationCache.ConcurrentDict.AddOrUpdate(key, deviation, (_, _) => deviation);
        }

        return deviation.value;
    }

    /// <inheritdoc/>
    public async Task<double> GetDurationEstimate(double agvVelocity, AgvAction action, Guid agvCorrelationId)
    {
        var agvPosition = _agvPositionRepository.GetPosition(agvCorrelationId).AgvPosition;
        if (action.IsMoveToCoordinate)
        {
            return EstimateDurationFromDistance((action.Target.Position!.X, action.Target.Position!.Y), (agvPosition.X, agvPosition.Y), agvVelocity);
        }
        var node = await _graphRepository.GetNodeAsync(action.Target.NodeId!);
        return EstimateDurationFromDistance((agvPosition.X, agvPosition.Y), (node.X, node.Y), agvVelocity);
    }

    private static double EstimateDurationFromDistance((float, float) source, (float, float) target, double velocity)
    {
        if (velocity == 0)
        {
            return 0;
        }
        double distance = Mathematics.EuclideanDistance(new Vector3(source.Item1, source.Item2, 0), new Vector3(target.Item1, target.Item2, 0));
        return distance / velocity;
    }

    /// <summary>
    /// Get key of in memory cache of deviations.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns></returns>
    public static string GetKey(Guid agvCorrelationId, string[] targetNodes)
    {
        return $"{agvCorrelationId}-{string.Join("-", targetNodes.OrderDescending())}";
    }

    /// <inheritdoc/>
    public async Task<double> AddNonRouteCosts(bool lastActionWasMoveToCoordinate, Models.ActionGroup.ActionGroup actionGroup, AgvAction action, Agv assignedAgv)
    {
        double duration = 0;
        if (!lastActionWasMoveToCoordinate && !action.IsMoveToCoordinate && actionGroup.IsMovingCargo)
        {
            // average of unaccounted for costs (e.g. cargo loading and unloading) for pick/drop actions
            duration += await GetAverageDurationEstimateDeviationAsync(actionGroup.AssignedAgv!.Value, actionGroup.GetTargets());
        }
    
        // costs for charging itself, just assume 1% / s, we don't have the charging rate here
        double chargeCostsPerCharge = 100 - assignedAgv.AgvStatus!.BatteryCharge;

        duration += actionGroup.Actions.Count(a => a.ActionType == ActionType.Charge) * chargeCostsPerCharge;

        // costs for action switching / state transitions of the Agv from one action to the next action
        duration += actionGroup.Actions.Count * ActionCosts.SwitchingActions;
        return duration;
    }
}