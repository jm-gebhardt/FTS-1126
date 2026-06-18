using System.Reflection;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Provides values of pre-defined vda5050 constants.
/// </summary>
[PublicAPI]
public static class Vda5050Specifications
{
    public const string Version = "2.1.0";

    public const string LifUploadFileType = "application/json";

    public static class ActionTypes
    {
        public const string Pick = "pick";
        public const string Drop = "drop";
        public const string DetectObject = "detectObject";
        public const string EnableMap = "enableMap";
        public const string FinePositioning = "finePositioning";
        public const string InitPosition = "initPosition";
        public const string StartCharging = "startCharging";
        public const string StopCharging = "stopCharging";
        public const string WaitForTrigger = "waitForTrigger";
        public const string CancelOrder = "cancelOrder";

        /// <summary>
        /// True, if the given action type is predefined in Vda5050.
        /// </summary>
        /// <param name="actionType">The type of the action.</param>
        public static bool IsVda5050Action(string actionType)
        {
            return typeof(ActionTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Any(f => f.FieldType == typeof(string) && (string)f.GetValue(null)! == actionType);
        }
    }

    public static class Parameters
    {
        public const string ObjectType = "objectType";
        public const string StationName = "stationName";
        public const string StationType = "stationType";
        public const string LoadHandlingDevice = "lhd";
        public const string MapId = "mapId";
        public const string MapVersion = "mapVersion";
        public const string X = "x";
        public const string Y = "y";
        public const string LastNodeId = "lastNodeId";
        public const string Theta = "theta";
        public const string TriggerType = "triggerType";
    }

    public static class Subtopics
    {
        public const string InstantActions = "instantActions";
        public const string Order = "order";
    }

    public static class OrderErrors
    {
        public const string OrderError = "orderError";
        public const string OrderUpdateError = "orderUpdateError";
        public const string ValidationError = "validationError";

        /// <summary>
        /// True, if the given error type is predefined for orders in Vda5050.
        /// </summary>
        /// <param name="errorType">The type of the error.</param>
        public static bool IsOrderError(string errorType)
        {
            return typeof(ActionTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Any(f => f.FieldType == typeof(string) && (string)f.GetValue(null)! == errorType);
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

