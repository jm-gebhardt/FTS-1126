using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// Represents the agv.
/// </summary>
public class Agv : IAgv
{
    private AgvState _currentState;
    private bool _isLocked;

    /// <inheritdoc />
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The group of agv settings the agv belongs to.
    /// </summary>
    public Guid AgvSettingsGroup { get; set; }

    /// <inheritdoc />
    public string SerialNumber { get; init; }

    /// <summary>
    /// The name of the agv.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Last received agv status.
    /// </summary>
    public AgvStatus? AgvStatus { get; set; }

    /// <summary>
    /// The mode of the agv.
    /// </summary>

    public AgvMode Mode { get; init; }

    /// <summary>
    /// The current state of the agv.
    /// </summary>
    public AgvState CurrentAgvState
    {
        get => _currentState;
        private set
        {
            bool hasChanged = value != _currentState;

            if (hasChanged)
            {
                PreviousAgvState = _currentState;
                TimeOfStateTransition = DateTime.UtcNow;
                _currentState = value;
            }
        }
    }

    /// <summary>
    /// The agv can start an automatic action.
    /// </summary>
    public bool CanStartAutomaticAction => Mode.Type == ModeType.Normal;

    /// <summary>
    /// The agv can start a manual pick/drop action.
    /// </summary>
    public bool CanStartManualPickDropAction => new List<ModeType>{ModeType.Maintenance, ModeType.Normal}.Contains(Mode.Type);

    /// <summary>
    /// The agv can start a manual operational action (Park, Charge, Move).
    /// </summary>
    public bool CanStartManualOperationalAction => new List<ModeType> { ModeType.Maintenance, ModeType.Normal, ModeType.UserMode }.Contains(Mode.Type);

    /// <summary>
    /// The agv should start manual action next.
    /// </summary>
    public bool ShouldStartManualAction => new List<ModeType> { ModeType.Maintenance, ModeType.UserMode }.Contains(Mode.Type);

    /// <summary>
    /// Gets the previous state of the agv.
    /// </summary>
    public AgvState PreviousAgvState { get; private set; } = AgvState.Initial;

    /// <summary>
    /// UTC timestamp when the agv data was requested.
    /// </summary>
    /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
    public DateTime? AgvDataRequestedLastTime { get; set; }

    /// <summary>
    /// UTC timestamp when the agv graph was requested.
    /// </summary>
    /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
    public DateTime? AgvGraphRequestedLastTime { get; set; }

    /// <summary>
    /// Returns true when there is a sign of life of the agv in regular time intervals,
    /// returns false when there is no sign of life of the agv for a certain time.
    /// </summary>
    public bool IsOnline { get; private set; }

    /// <summary>
    /// Is the agv locked?
    /// </summary>
    public bool IsLocked
    {
        get => Mode.Type == ModeType.Locked;
        private set
        {
            bool hasChanged = value != _isLocked;

            if (hasChanged)
            {
                TimeOfLockStateChanged = DateTime.UtcNow;
                _isLocked =  Mode.Type == ModeType.Locked;
            }
        }
    }

    /// <summary>
    /// Is the agv currently charging?
    /// </summary>
    public bool IsCharging => AgvStatus?.Charging ?? false;

    /// <summary>
    /// Does the agv need maintenance?
    /// </summary>
    public bool NeedsMaintenance { get; private set; }

    /// <summary>
    /// Returns true if the agv has any error and or is not ready to drive.
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Returns true if the state of the agv has changed.
    /// </summary>
    public bool StateChanged => CurrentAgvState != PreviousAgvState && (TimeOfStateTransition - DateTime.UtcNow).Seconds < 2;

    /// <summary>
    /// The agv is ready.
    /// An agv is ready when lastNodeId for AgvStatus is set and agv has no errors assigned.
    /// </summary>
    public bool IsReady => IsReadyStatic.Compile().Invoke(this);

    /// <summary>
    /// Token used by the send status update schedule
    /// </summary>
    public Guid? AgvOfflineTokenId { get; set; }

    /// <summary>
    /// Gets the time of the last state transition.
    /// </summary>
    public DateTime TimeOfStateTransition { get; set; }

    /// <summary>
    /// Gets the time of the last lock state change.
    /// </summary>
    public DateTime TimeOfLockStateChanged { get; set; }

    /// <summary>
    /// Returns true if the graph of this agv exists.
    /// </summary>
    public bool GraphExists { get; set; }

    /// <summary>
    /// Returns true if the data of this agv exists.
    /// </summary>
    public bool DataExist { get; set; }

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are still null.
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private TimeSpan AgvRequestTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The threshold for considering a battery change significant.
    /// </summary>
    public const double BatteryChargeChangedThreshold = 0.01d;

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are already given
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private TimeSpan DailyRequestUpdateInterval { get; } = TimeSpan.FromHours(24);

    /// <inheritdoc cref="Agv"/>
    public Agv(Guid correlationId,
        Guid settingsGroup,
        string serialNumber,
        AgvStatus? agvStatus = null,
        bool isLocked = false,
        string? agvName = null
    )
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        AgvSettingsGroup = settingsGroup;
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Name = agvName ?? serialNumber;
        AgvStatus = agvStatus;
        IsLocked = isLocked;
        NeedsMaintenance = false;
        CurrentAgvState = AgvState.Initial;
        Mode = new AgvMode(ModeType.Normal, CorrelationId);
    }

    /// <summary>
    /// EfCore ctor.
    /// </summary>
    [UsedImplicitly]
    public Agv(string serialNumber, Guid settingsGroup, string? agvName = null)
    {
        CorrelationId = serialNumber.ToGuid();
        AgvSettingsGroup = settingsGroup;
        SerialNumber = serialNumber;
        Name = agvName ?? serialNumber;
        CurrentAgvState = AgvState.Initial;
        Mode = new AgvMode(ModeType.Normal, CorrelationId);
    }

    /// <summary>
    /// Agv ctor used for serialization.
    /// </summary>
    public Agv()
    {
        SerialNumber = null!;
        Name = null!;
        CurrentAgvState = AgvState.Initial;
        Mode = new AgvMode(ModeType.Normal, CorrelationId);
    }

    /// <summary>
    /// Check if the agv is eligible for charging.
    /// </summary>
    /// <param name="settingsReadModel">The global agv settingsReadModel.</param>
    public bool CheckShouldCharge(AgvSettingsReadModel settingsReadModel)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return AgvStatus.BatteryCharge <= settingsReadModel.BatteryLevelLow;
    }

    /// <summary>
    /// Check if the agv needs charging.
    /// </summary>
    /// <param name="settingsReadModel">The global agv settingsReadModel.</param>
    public bool CheckMustCharge(AgvSettingsReadModel settingsReadModel)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return AgvStatus.BatteryCharge <= settingsReadModel.BatteryLevelCritical;
    }

    /// <summary>
    /// Discovers the agv.
    /// </summary>
    /// <param name="graphExists">True if a graph already exists.</param>
    public void TransitionToDiscovered(bool graphExists)
    {
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Discovered);
        GraphExists = graphExists;
    }

    /// <summary>
    /// Check to see if the difference in battery charge is bigger than <see cref="BatteryChargeChangedThreshold"/>.
    /// </summary>
    /// <returns></returns>
    private bool CheckBatteryChargeChange(double newBatteryCharge)
    {
        if (AgvStatus == null)
        {
            return false;
        }
        return Math.Abs(AgvStatus.BatteryCharge - newBatteryCharge) > BatteryChargeChangedThreshold;
    }

    private bool CheckLastNodeIdChanged(string? newLastNodeId)
    {
        if (AgvStatus == null)
        {
            return !string.IsNullOrEmpty(newLastNodeId);
        }
        return AgvStatus.LastNodeId != newLastNodeId;
    }

    private bool SetIdle()
    {
        var previousState = CurrentAgvState;
        CurrentAgvState = AgvState.Idle;
        return previousState != CurrentAgvState;
    }

    /// <summary>
    /// Update the agv status.
    /// </summary>
    /// <param name="newStatus"></param>
    public (bool batteryChargeChanged, bool lastNodeIdChanged) UpdateAgvStatus(AgvStatus newStatus)
    {
        bool batteryChargeChanged = CheckBatteryChargeChange(newStatus.BatteryCharge);
        if (!batteryChargeChanged && AgvStatus != null)
        {
            newStatus = new AgvStatus(newStatus.CorrelationId,
                newStatus.OrderId,
                newStatus.Busy,
                newStatus.Automatic,
                // use old battery charge, do not update if it is not considered significant
                AgvStatus.BatteryCharge,
                newStatus.Charging,
                newStatus.Disturbed,
                newStatus.LastNodeId,
                newStatus.BatteryVoltage,
                newStatus.BatteryCurrent,
                newStatus.BatteryTemperature,
                newStatus.EstimatedFullyChargedTime,
                newStatus.LastBatteryReplacement
            );
        }

        bool lastNodeIdChanged = CheckLastNodeIdChanged(newStatus.LastNodeId);
        AgvStatus = newStatus;
        return (batteryChargeChanged, lastNodeIdChanged);
    }

    /// <summary>
    /// Try to accept the agv, i.e. set it to idle if it is not idling yet, busy, or charging.
    /// </summary>
    /// <returns>True if the state has changed</returns>
    public (bool Accepted, bool StateChanged) TryAccept()
    {
        if (new[] { AgvState.Idle, AgvState.Busy }.Contains(CurrentAgvState))
        {
            return (false, false);
        }
        bool stateChanged = SetIdle();
        return (true, stateChanged);
    }

    /// <summary>
    /// Transition to idle state.
    /// </summary>
    /// <returns>True if the state has changed</returns>
    public bool TransitionToIdle()
    {
        var previousState = CurrentAgvState;
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Idle);
        return previousState != CurrentAgvState;
    }

    /// <summary>
    /// Force the transition to idle state irrespective of previous state.
    /// </summary>
    /// <returns>True if the state has changed</returns>
    public bool ForceIdle()
    {
        return SetIdle();
    }

    /// <summary>
    /// Try to become busy.
    /// </summary>
    /// <returns></returns>
    public bool TryTransitionToBusy(bool ignoreLocked)
    {
        bool isAvailable = IsAvailable(ignoreLocked: ignoreLocked);
        if (!isAvailable)
        {
            return isAvailable;
        }
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.Busy);
        return isAvailable;
    }

    /// <summary>
    /// Checks if the agv is available.
    /// </summary>
    /// <param name="ignoreLocked">If locked should be ignored.</param>
    public bool IsAvailable(bool ignoreLocked = false)
    {
        bool unlocked = !IsLocked || ignoreLocked;
        bool idle = CurrentAgvState == AgvState.Idle && !AgvStatus!.Busy;
        return IsReady && idle && unlocked;
    }

    /// <summary>
    /// Lock the agv.
    /// </summary>
    public void Lock(string? reason=null, string? setter = null)
    {
        Mode.Switch(ModeType.Locked, reason, setter) ;
        IsLocked = true;
    }

    /// <summary>
    /// Set IsLocked to false and sets agv to idle state.
    /// </summary>
    public void Unlock(string? setter = null)
    {
        SetIdle();
        Mode.Switch(ModeType.Normal, modeModeSetter: setter);
        IsLocked = false;
    }

    /// <summary>
    /// Set the agv into user mode.
    /// </summary>
    public void SetUserMode(string? setter = null)
    {
        Mode.Switch(ModeType.UserMode, modeModeSetter: setter);
        SetIdle();
    }

    /// <summary>
    /// Set the agv into normal mode.
    /// </summary>
    public void SetNormalMode(string? setter = null)
    {
        Mode.Switch(ModeType.Normal, modeModeSetter: setter);
        SetIdle();
    }

    /// <summary>
    /// Set the agv into maintenance mode.
    /// </summary>
    public void SetMaintenanceMode(string? reason, string? setter = null)
    {
        Mode.Switch(ModeType.Maintenance, reason: reason, modeModeSetter: setter);
        SetIdle();
    }

    /// <summary>
    /// Checks if it is time and necessary to request a graph.
    /// </summary>
    public bool ShouldRequestGraph()
    {
        var secondsSinceLastRequest = TimeSpan.MaxValue;
        if (AgvGraphRequestedLastTime != null)
        {
            secondsSinceLastRequest = DateTime.UtcNow - (AgvGraphRequestedLastTime ?? DateTime.MinValue);
        }

        bool shouldBeRequested = !GraphExists || secondsSinceLastRequest > DailyRequestUpdateInterval;
        if (shouldBeRequested)
        {
            AgvGraphRequestedLastTime = DateTime.UtcNow;
        }
        return shouldBeRequested;
    }

    /// <summary>
    ///  Checks if it is time to request new agv data.
    /// </summary>
    /// <returns></returns>
    public bool ShouldRequestData()
    {
        var secondsSinceLastRequest = TimeSpan.MaxValue;
        if (AgvDataRequestedLastTime != null)
        {
            secondsSinceLastRequest = DateTime.UtcNow - (AgvDataRequestedLastTime ?? DateTime.MinValue);
        }
        var timeout = DataExist ? DailyRequestUpdateInterval : AgvRequestTimeout;

        bool shouldBeRequested = secondsSinceLastRequest > timeout;
        if (shouldBeRequested)
        {
            AgvDataRequestedLastTime = DateTime.UtcNow;
        }
        return shouldBeRequested;
    }

    /// <summary>
    /// Go offline.
    /// </summary>
    public void GoOffline(Guid offlineToken)
    {
        if (AgvOfflineTokenId == null || AgvOfflineTokenId!.Value != offlineToken)
        {
            return;
        }
        IsOnline = false;
        AgvOfflineTokenId = default;
    }

    /// <summary>
    /// Sets agv online.
    /// </summary>
    public void SetOnline(Guid offlineToken)
    {
        AgvOfflineTokenId = offlineToken;
        IsOnline = true;
    }

    /// <summary>
    /// Static expression to wrap <see cref="IsReady"/> for LINQ to SQL conversion.
    /// </summary>
    public static Expression<Func<Agv, bool>> IsReadyStatic
    {
        get
        {
            return agv => agv.AgvStatus != null && agv.AgvStatus!.LastNodeId != null && !agv.HasError;
        }
    }
}