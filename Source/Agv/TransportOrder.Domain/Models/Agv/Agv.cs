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
    /// The id of the agv type.
    /// </summary>
    public Guid? AgvTypeId { get; private set; }

    /// <summary>
    /// True, if the type change has been confirmed. Only applicable if it needs confirmation.
    /// </summary>
    public bool? TypeChangeConfirmed { get; private set; }

    /// <summary>
    /// The time span an agv can be offline without assigned nodes or action groups being cleared.
    /// </summary>
    public static TimeSpan OfflineTolerance => TimeSpan.FromMinutes(5);

    /// <summary>
    /// The current state of the agv.
    /// </summary>
    public AgvState CurrentAgvState
    {
        get;
        private set
        {
            bool hasChanged = value != field;

            if (hasChanged)
            {
                PreviousAgvState = field;
                TimeOfStateTransition = DateTime.UtcNow;
                field = value;
            }
        }
    }

    /// <summary>
    /// The agv is queueing.
    /// </summary>
    public bool IsQueueing { get; set; }

    /// <summary>
    /// The agv can start an automatic action.
    /// </summary>
    public bool CanStartAutomaticAction => Mode.Type == ModeType.Normal;

    /// <summary>
    /// The agv can start a manual pick/drop action.
    /// </summary>
    public bool CanStartManualPickDropAction => new List<ModeType> { ModeType.Maintenance, ModeType.Normal }.Contains(Mode.Type);

    /// <summary>
    /// The agv can start a manual operational action (Park, Charge, Move).
    /// </summary>
    public bool CanStartManualOperationalAction => new List<ModeType> { ModeType.Maintenance, ModeType.Normal, ModeType.UserMode }.Contains(Mode.Type);

    /// <summary>
    /// True, if the agv is in or pending maintenance mode.
    /// </summary>

    public bool IsInMaintenance => Mode.Type == ModeType.Maintenance || Mode.PendingType == ModeType.Maintenance;

    /// <summary>
    /// The agv should start manual action next.
    /// </summary>
    public bool ShouldStartManualAction =>
        new List<ModeType> { ModeType.Maintenance, ModeType.UserMode }.Contains(Mode.Type) ||
        new List<ModeType> { ModeType.Maintenance, ModeType.UserMode }.Contains(Mode.PendingType.GetValueOrDefault());

    /// <summary>
    /// True, if the agv is going to switch to a different mode (apart from reset to normal).
    /// </summary>

    public bool HasPendingModeSwitch => Mode.PendingType != null && Mode.PendingType != ModeType.Normal;

    /// <summary>
    /// True, if the agv can not start a new action of the group.
    /// </summary>
    public bool CanNotStartNewAction => HasPendingModeSwitch && Mode.CurrentActionGroupPolicy != CurrentActionGroupPolicy.FinishActionGroup;

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
    /// UTC timestamp when the agv zones was requested.
    /// </summary>
    /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
    public DateTime? AgvZonesRequestedLastTime { get; set; }

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
            bool hasChanged = value != IsLocked;

            if (hasChanged)
            {
                TimeOfLockStateChanged = DateTime.UtcNow;
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
    /// Returns true if the zones of this agv exist.
    /// </summary>
    public bool ZonesExist { get; set; }

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are still null.
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private TimeSpan AgvRequestTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time that has to pass before resending a request for agv zones.
    /// </summary>
    private TimeSpan AgvZonesRequestTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The threshold for considering a battery charge change (decreasing) significant [%].
    /// </summary>
    public const double BatteryChargeDischargingThreshold = 0.1d;

    /// <summary>
    /// The threshold for considering a battery charge change (increasing) significant [%].
    /// </summary>
    public const double BatteryChargeChargingThreshold = 0.5d;

    /// <summary>
    /// The threshold for considering a battery voltage change significant [V].
    /// </summary>
    public const double BatteryVoltageThreshold = 0.5d;

    /// <summary>
    /// The threshold for considering a battery current change when idle significant [A].
    /// </summary>
    public const double BatteryCurrentIdleThreshold = 0.5d;

    /// <summary>
    /// The threshold for considering a battery current change when busy significant [A].
    /// </summary>
    public const double BatteryCurrentBusyThreshold = 5d;

    /// <summary>
    /// The threshold for considering a battery temperature change significant [°C].
    /// </summary>
    public const double BatteryTemperatureThreshold = 0.5d;

    /// <summary>
    /// The threshold for considering a battery datetime change (LastBatteryReplacement, EstimatedFullyChargedTime) significant.
    /// </summary>
    public const long BatteryStatusSecondsThreshold = 60;

    /// <summary>
    /// The threshold for considering an operating data total distance change significant [km].
    /// </summary>
    public const float OperatingDataTotalDistanceThreshold = 0.1f;

    /// <summary>
    /// The threshold for considering an operating data total lifetime change significant [h].
    /// </summary>
    public const float OperatingDataTotalLifetimeThreshold = 0.25f;

    /// <summary>
    /// Time that has to pass before resending a request telegram in case AgvGraph or AgvData are already given
    /// </summary>
    /// <remarks>Interval indicated in seconds</remarks>
    private TimeSpan DailyRequestUpdateInterval { get; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Time that has to pass before resending a graph request telegram in case graph is not given
    /// </summary>
    private TimeSpan GraphRequestUpdateInterval { get; } = TimeSpan.FromSeconds(10);

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
        NeedsMaintenance = false;
        CurrentAgvState = AgvState.Initial;
        Mode = new AgvMode(ModeType.Normal, CorrelationId);
        IsLocked = isLocked;
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

    /// <inheritdoc cref="Agv"/>
    public Agv(Guid correlationId, string serialNumber, bool hasValidConfiguration)
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        CurrentAgvState = hasValidConfiguration? AgvState.Discovered : AgvState.InvalidConfiguration;
        Name = SerialNumber;
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
    /// Set agv configuration to invalid.
    /// </summary>
    public void Invalidate()
    {
        CurrentAgvState = CurrentAgvState.TransitionTo(AgvState.InvalidConfiguration);
    }

    /// <summary>
    /// Update the type of the agv.
    /// </summary>
    /// <param name="agvType">The new agv type.</param>
    /// <param name="needsConfirmation">True, if the type change needs confirmation.</param>
    /// <param name="isValid">True, if the new type configuration is valid.</param>
    public void UpdateType(Guid? agvType, bool needsConfirmation, bool isValid)
    {
        AgvTypeId = agvType;
        if (needsConfirmation)
        {
            TypeChangeConfirmed = false;
        }
        if (!isValid)
        {
            Invalidate();
        }
    }

    /// <summary>
    /// Acknowledge the type change of the agv.
    /// </summary>
    public void AcknowledgeTypeChange()
    {
        TypeChangeConfirmed = true;
        TransitionToIdle();
    }

    /// <summary>
    /// Check to see if parts of the battery status other than charge level changed.
    /// </summary>
    /// <returns></returns>
    private bool CheckBatteryStatusChange(AgvStatus newStatus)
    {
        if (AgvStatus == null)
        {
            return true;
        }
        var newEstimatedFullyChargedTime = newStatus.EstimatedFullyChargedTime ?? DateTime.MinValue;
        var estimatedFullyChargeTime = AgvStatus.EstimatedFullyChargedTime ?? DateTime.MinValue;
        var newLastBatteryReplacement = newStatus.LastBatteryReplacement ?? DateTime.MinValue;
        var lastBatteryReplacement = AgvStatus.LastBatteryReplacement ?? DateTime.MinValue;

        double batteryCurrentThreshold = AgvStatus.Busy ? BatteryCurrentBusyThreshold : BatteryCurrentIdleThreshold;
        return newStatus.Charging != AgvStatus.Charging
               || Math.Abs(newStatus.BatteryCurrent - AgvStatus.BatteryCurrent) > batteryCurrentThreshold
               || Math.Abs(newStatus.BatteryTemperature - AgvStatus.BatteryTemperature) > BatteryTemperatureThreshold
               || Math.Abs(newStatus.BatteryVoltage - AgvStatus.BatteryVoltage) > BatteryVoltageThreshold
               || Math.Abs((newEstimatedFullyChargedTime - estimatedFullyChargeTime).TotalSeconds) > BatteryStatusSecondsThreshold
               || Math.Abs((newLastBatteryReplacement - lastBatteryReplacement).TotalSeconds) > BatteryStatusSecondsThreshold;
    }

    /// <summary>
    /// Check to see if the difference in battery charge is bigger than <see cref="BatteryChargeChargingThreshold"/> or <see cref="BatteryChargeDischargingThreshold"/>.
    /// </summary>
    /// <returns></returns>
    private bool CheckBatteryChargeChange(double newBatteryCharge)
    {
        if (AgvStatus == null)
        {
            return false;
        }

        double threshold = AgvStatus.Charging ? BatteryChargeChargingThreshold : BatteryChargeDischargingThreshold;
        return Math.Abs(AgvStatus.BatteryCharge - newBatteryCharge) > threshold;
    }

    private bool CheckLastNodeIdChanged(Guid? newLastNodeId)
    {
        if (AgvStatus == null)
        {
            return newLastNodeId != null && newLastNodeId != Guid.Empty;
        }
        return AgvStatus.LastNodeId != newLastNodeId;
    }

    private bool CheckOperatingDataTotalDistanceChanged(float newTotalDistance)
    {
        if (AgvStatus == null)
        {
            return true;
        }

        return Math.Abs(newTotalDistance - AgvStatus.TotalDistance) > OperatingDataTotalDistanceThreshold;
    }

    private bool CheckOperatingDataTotalLifetimeChanged(float newTotalLifetime)
    {
        if (AgvStatus == null)
        {
            return true;
        }

        return Math.Abs(newTotalLifetime - AgvStatus.TotalLifetime) > OperatingDataTotalLifetimeThreshold;
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
    public (bool batteryStatusChanged, bool batteryChargeChanged, bool lastNodeIdChanged, bool operatingDataChanged) UpdateAgvStatus(AgvStatus newStatus)
    {
        bool batteryChargeChanged = CheckBatteryChargeChange(newStatus.BatteryCharge);
        bool operatingDataTotalDistanceChanged = CheckOperatingDataTotalDistanceChanged(newStatus.TotalDistance);
        bool operatingDataTotalLifetimeChanged = CheckOperatingDataTotalLifetimeChanged(newStatus.TotalLifetime);
        if (AgvStatus != null)
        {
            // LastKnownPosition is intentionally NOT carried forward here.
            // It is a DB-only field managed via tracked entity operations in AgvPositionRepository
            // (PersistPositionAsync / ClearPersistedPositionAsync), not through in-memory domain state.
            newStatus = new AgvStatus(newStatus.CorrelationId,
                newStatus.OrderId,
                newStatus.Busy,
                newStatus.Automatic,
                // use old battery charge, it is not considered significant
                batteryChargeChanged ? newStatus.BatteryCharge : AgvStatus.BatteryCharge,
                newStatus.Charging,
                newStatus.Disturbed,
                newStatus.LastNodeId,
                newStatus.BatteryVoltage,
                newStatus.BatteryCurrent,
                newStatus.BatteryTemperature,
                newStatus.EstimatedFullyChargedTime,
                AgvStatus.LastBatteryReplacement,
                newStatus.ZoneIds,
                newStatus.StationIds,
                // use old operating data if it is not considered significant
                operatingDataTotalDistanceChanged ? newStatus.TotalDistance : AgvStatus.TotalDistance,
                operatingDataTotalLifetimeChanged ? newStatus.TotalLifetime : AgvStatus.TotalLifetime
            );
        }

        bool batteryStatusChanged = batteryChargeChanged || CheckBatteryStatusChange(newStatus);
        bool operatingDataChanged = operatingDataTotalDistanceChanged || operatingDataTotalLifetimeChanged;
        bool lastNodeIdChanged = CheckLastNodeIdChanged(newStatus.LastNodeId);
        AgvStatus = newStatus;
        return (batteryStatusChanged, batteryChargeChanged, lastNodeIdChanged, operatingDataChanged);
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
    public bool TryTransitionToBusy(bool ignoreLocked, bool ignoreError = false)
    {
        bool isAvailable = IsAvailable(ignoreLocked: ignoreLocked, ignoreError: ignoreError);
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
    /// <param name="ignoreError">If error should be ignored.</param>
    public bool IsAvailable(bool ignoreLocked = false, bool ignoreError = false)
    {
        bool unlocked = !IsLocked || ignoreLocked;
        bool idle = CurrentAgvState == AgvState.Idle && !AgvStatus!.Busy;
        return (IsReady || (ignoreError && IsReadyIgnoringError)) && idle && unlocked;
    }

    /// <summary>
    /// Prepare transition to locked mode.
    /// </summary>
    public void PrepareLockedMode(
        LockReason? lockReason = null,
        string? reasonDescription = null,
        string? initiatingUser = null,
        CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.FinishAction,
        AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.Reset,
        bool cancelManualActions = false)
    {
        Mode.PrepareSwitch(ModeType.Locked, lockReason, null, reasonDescription, initiatingUser, currentActionPolicy, assignedActionPolicy, cancelManualActions, DateTime.UtcNow);
    }

    /// <summary>
    /// Unlock the agv.
    /// </summary>
    public void Unlock(string? initiatingUser = null)
    {
        SetIdle();
        Mode.PrepareSwitch(ModeType.Normal, initiatingUser: initiatingUser, modeChangeTime: DateTime.UtcNow);
        Mode.Switch();
        IsLocked = false;
    }

    /// <summary>
    /// Prepare transition to user mode.
    /// </summary>
    public void PrepareUserMode(string? initiatingUser = null, CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.Pause, AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.LeaveAssigned, bool cancelManualActions = false)
    {
        Mode.PrepareSwitch(ModeType.UserMode, initiatingUser: initiatingUser, currentActionPolicy: currentActionPolicy, assignedActionPolicy: assignedActionPolicy, cancelManualActions: cancelManualActions, modeChangeTime: DateTime.UtcNow);
    }

    /// <summary>
    /// Set the agv into normal mode.
    /// </summary>
    public void SetNormalMode(string? initiatingUser = null)
    {
        Mode.PrepareSwitch(ModeType.Normal, initiatingUser: initiatingUser, modeChangeTime: DateTime.UtcNow);
        Mode.Switch();
        SetIdle();
        IsLocked = false;
    }

    /// <summary>
    /// Prepare transition to maintenance mode.
    /// </summary>
    public void PrepareMaintenanceMode(
        MaintenanceReason? maintenanceReason,
        string? reasonDescription,
        string? initiatingUser = null,
        CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.Pause,
        AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.Reset,
        bool cancelManualActions = false)
    {
        Mode.PrepareSwitch(ModeType.Maintenance, maintenanceReason: maintenanceReason, reasonDescription: reasonDescription, initiatingUser: initiatingUser, currentActionPolicy: currentActionPolicy, assignedActionPolicy: assignedActionPolicy, cancelManualActions: cancelManualActions, modeChangeTime: DateTime.UtcNow);
    }

    /// <summary>
    /// Perform switch to pending mode.
    /// </summary>
    public void SwitchMode()
    {
        Mode.Switch();
        SetIdle();
        if (Mode.Type == ModeType.Locked)
        {
            IsLocked = true;
        }
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

        bool shouldBeRequested = (!GraphExists && secondsSinceLastRequest > GraphRequestUpdateInterval) || (GraphExists && secondsSinceLastRequest > DailyRequestUpdateInterval);
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
    /// Checks if it is time to request new agv zones.
    /// </summary>
    /// <returns>True, if zones need to be requested.</returns>
    public bool ShouldRequestZones()
    {
        var secondsSinceLastRequest = TimeSpan.MaxValue;
        if (AgvZonesRequestedLastTime != null)
        {
            secondsSinceLastRequest = DateTime.UtcNow - (AgvZonesRequestedLastTime ?? DateTime.MinValue);
        }
        var timeout = DataExist ? DailyRequestUpdateInterval : AgvZonesRequestTimeout;

        bool shouldBeRequested = secondsSinceLastRequest > timeout;
        if (shouldBeRequested)
        {
            AgvZonesRequestedLastTime = DateTime.UtcNow;
        }
        return shouldBeRequested;
    }

    /// <summary>
    /// Go offline.
    /// </summary>
    public void GoOffline()
    {
        IsOnline = false;
    }

    /// <summary>
    /// Sets agv online.
    /// </summary>
    public void SetOnline()
    {
        IsOnline = true;
    }

    /// <summary>
    /// Static expression to wrap <see cref="IsReady"/> for LINQ to SQL conversion.
    /// </summary>
    public static Expression<Func<Agv, bool>> IsReadyStatic
    {
        get
        {
            return agv =>
                agv.AgvStatus != null &&
                agv.AgvStatus.LastNodeId != Guid.Empty &&
                !agv.HasError &&
                agv.IsOnline;
        }
    }

    /// <summary>
    /// If the agv is ready ignoring if it has an error.
    /// </summary>
    public bool IsReadyIgnoringError => AgvStatus?.LastNodeId is { } g && g != Guid.Empty && IsOnline;
}