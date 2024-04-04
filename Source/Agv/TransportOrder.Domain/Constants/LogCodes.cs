namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Constants;

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
}