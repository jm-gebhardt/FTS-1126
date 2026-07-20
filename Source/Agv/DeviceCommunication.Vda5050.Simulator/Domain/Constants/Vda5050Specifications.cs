using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Provides values of pre-defined vda5050 constants.
/// </summary>
[PublicAPI]
public static class Vda5050Specifications
{
    public const string Version = "2.1.0";
    
    public static class ActionTypes
    {
        public const string Pick = "pick";
        public const string Drop = "drop";
        public const string InitPosition = "initPosition";
        public const string StartCharging = "startCharging";
        public const string StopCharging = "stopCharging";
        public const string CancelOrder = "cancelOrder";
    }

    public static class Parameters
    {
        public const string LoadHandlingDevice = "lhd";
        public const string X = "x";
        public const string Y = "y";
        public const string LastNodeId = "lastNodeId";
        public const string Theta = "theta";
    }

    public static class Subtopics
    {
        public const string State = "state";
        public const string Connection = "connection";
        public const string Visualization = "visualization";
    }

    public static class OrderErrors
    {
        public const string OrderError = "orderError";
        public const string OrderUpdateError = "orderUpdateError";
        public const string ValidationError = "validationError";
    }

    public static class OrderWarnings
    {
        public const string MobileRobotNotAvailable = "MOBILE_ROBOT_NOT_AVAILABLE";
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

