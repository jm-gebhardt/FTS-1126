namespace Gebhardt.Acc.Common.Domain.Constants;

/// <summary>
/// Provides static log codes 
/// </summary>
public static class LogCodes
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member  
    // Battery 
    public const int BatteryCriticalChargeLevel = 200;
    public const int BatteryStartedCharging = 201;
    public const int BatteryLowChargeLevel = 202;
    public const int BatteryMinChargeLevel = 203;
    public const int BatteryMaxChargeLevel = 204;
    public const int BatteryFullyCharged = 205;
    public const int BatteryError = 206;
    public const int BatteryWarning = 207;
    public const int BatteryNeedsChargingAllOccupied = 208;

    // Agv 
    public const int AgvDiscovered = 300;
    public const int AgvInformationReceived = 301;
    public const int AgvGraphReceived = 302;
    public const int AgvReady = 303;
    public const int AgvOnline = 304;
    public const int AgvOffline = 305;
    public const int AgvError = 306;
    public const int AgvWarning = 307;
    public const int AgvLocked = 308;
    public const int AgvUnlocked = 309;
    public const int AgvCargoUpdated = 310;
    public const int AgvIdle = 311;
    public const int AgvSettingUpdated = 312;
    public const int AgvSettingsReset = 313;
    public const int AgvModeChanged = 314;
    public const int AgvReachedTrigger = 315;
    public const int AgvModeChangePending = 316;
    public const int AgvInformation = 317;
    public const int AgvMaintenanceLogCreated = 318;
    public const int AgvErrorReset = 319;
    public const int AgvAccept = 320;
    public const int AgvGraphRequested = 321;
    public const int AgvDataRequested = 322;
    public const int AgvZonesRequested = 323;
    public const int AgvManualMove = 324;
    public const int AgvManualCharge = 325;
    public const int AgvStartInstantAction = 326;
    public const int AgvRemoveFromZone = 327;
    public const int AgvRemoveFromNode = 328;
    public const int AgvOperatorInteraction = 329;
    public const int AgvRemoveFromQueue = 336;

    // Order
    public const int OrderCreated = 100;
    public const int OrderAgvAssigned = 101;
    public const int OrderStarted = 102;
    public const int OrderActionCompleted = 103;
    public const int OrderActionStarted = 104;
    public const int OrderCompleted = 105;
    public const int OrderInvalid = 106;
    public const int OrderAborted = 107;
    public const int OrderCanceled = 108;
    public const int OrderIncomplete = 109;
    public const int OrderPaused = 110;
    public const int OrderResumed = 111;
    public const int OrderWithLockedStation = 112;
    public const int OrderReset = 113;
    public const int OrderReassigned = 114;
    public const int OrderQueued = 115;
    public const int OrderTargetSet = 116;
    public const int OrderOperatorInteraction = 117;
    public const int AgvSentActionStartedForOrder = 118;
    public const int AgvSentActionCompletedForOrder = 119;
    public const int AgvSentActionAbortedForOrder = 120;
    public const int AgvSentActionAbortionFailedForOrder = 121;
    public const int AgvSentActionUpdatedForOrder = 122;
    public const int AgvSentActionFailedForOrder = 123;
    public const int OrderRetracted = 124;
    public const int OrderFailed = 125;
    public const int OrderActionResent = 126;
    public const int OrderAgvWarning = 127;
    public const int OrderAgvError = 128;

    //Station
    public const int StationStateChanged = 400;
    public const int StationAgvDropping = 401;
    public const int StationAgvPicking = 402;
    public const int StationAgvCharging = 403;
    public const int StationAgvMoving = 404;
    public const int StationAgvReached = 405;
    public const int StationAgvLeft = 406;
    public const int StationLocked = 407;
    public const int StationUnlocked = 408;
    public const int StationUnblocked = 409;
    public const int StationOperatorInteraction = 410;

    //System
    public const int SystemOperatorInteraction = 500;
    public const int SystemAgvDefaultSettingUpdated = 501;
    public const int SystemMapBackgroundImageUpdated = 502;
    public const int SystemMapBackgroundImageDeleted = 503;
    public const int SystemMapUpdated = 504;
    public const int SystemNodeApproachingPropertiesUpdated = 505;
    public const int SystemQueueAdded = 506;
    public const int SystemQueueDeleted = 507;
    public const int SystemRestrictedAreaAdded = 508;
    public const int SystemRestrictedAreaDeleted = 509;
    public const int SystemRestrictedAreaUpdated = 510;
    public const int SystemTriggerAssignedToLocation = 511;
    public const int SystemTriggerLocationRemoved = 512;
    public const int SystemTriggerLocationConfigurationUpdated = 513;
    public const int SystemTriggerLocationConfigurationReset = 514;
    public const int SystemTriggerDefaultConfigurationUpdated = 515;
    public const int SystemTriggerMarkedForDeletion = 516;
}