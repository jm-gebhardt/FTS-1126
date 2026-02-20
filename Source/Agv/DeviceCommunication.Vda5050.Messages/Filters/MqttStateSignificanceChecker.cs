using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

// ReSharper disable EnforceIfStatementBraces

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Filters;

/// <summary>
/// Filter by changes in <see cref="MqttState"/>.
/// </summary>
public class MqttStateSignificanceChecker : ISignificanceChecker<MqttState>
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(MqttState a, MqttState b)
    {
        if (!EqualsIgnoreCase(a.SerialNumber, b.SerialNumber)) return true;
        if (!EqualsIgnoreCase(a.OrderId, b.OrderId)) return true;
        if (!AreListsEqual(a.ActionStates, b.ActionStates, (x, y) => x.Equals(y))) return true;
        if (!AreObjectsEqual(a.AgvPosition, b.AgvPosition)) return true;
        if (!AreObjectsEqual(a.BatteryState, b.BatteryState)) return true;
        if (!AreFloatsEqual(a.DistanceSinceLastNode, b.DistanceSinceLastNode)) return true;
        if (a.Driving != b.Driving) return true;
        if (!AreListsEqual(a.EdgeStates, b.EdgeStates, EdgeStateEqual)) return true;
        if (!AreListsEqual(a.Errors, b.Errors, ErrorEqual)) return true;
        if (!AreIntsEqual(a.HeaderId, b.HeaderId)) return true;
        if (!AreListsEqual(a.Information, b.Information, InformationEqual)) return true;
        if (!EqualsIgnoreCase(a.LastNodeId, b.LastNodeId)) return true;
         if (a.LastNodeSequenceId != b.LastNodeSequenceId) return true;
        if (!AreListsEqual(a.Loads, b.Loads, (x, y) => x.Equals(y))) return true;
        if (!EqualsIgnoreCase(a.Manufacturer, b.Manufacturer)) return true;
        if (a.NewBaseRequest != b.NewBaseRequest) return true;
        if (!AreListsEqual(a.NodeStates, b.NodeStates, (x, y) => x.Equals(y))) return true;
        if (a.OperatingMode != b.OperatingMode) return true;
        if (a.OrderUpdateId != b.OrderUpdateId) return true;
        if (a.Paused != b.Paused) return true;
        if (!SafetyStateEqual(a.SafetyState, b.SafetyState)) return true;
        if (!AreTimestampsEqual(a.Timestamp, b.Timestamp)) return true;
         if (!AreObjectsEqual(a.Velocity, b.Velocity)) return true;
        if (!EqualsIgnoreCase(a.Version, b.Version)) return true;
        if (!EqualsIgnoreCase(a.ZoneSetId, b.ZoneSetId)) return true;
        return false;
    }

    private static bool EqualsIgnoreCase(string? x, string? y) => string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);

    private static bool AreIntsEqual(int? x, int? y) => x.GetValueOrDefault() == y.GetValueOrDefault();

    private static bool AreFloatsEqual(float? x, float? y) => Math.Abs(x.GetValueOrDefault() - y.GetValueOrDefault()) < Mathematics.FloatComparator;

    private static bool AreTimestampsEqual(DateTimeOffset x, DateTimeOffset y) => x.UtcDateTime == y.UtcDateTime;

    private static bool AreObjectsEqual<T>(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Equals(y);
    }

    private static bool AreListsEqual<T>(IList<T>? x, IList<T>? y, Func<T, T, bool> itemEqual)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Count != y.Count) return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!itemEqual(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ControlPointEqual(MqttControlPoint a, MqttControlPoint b)
    {
        return AreFloatsEqual(a.X, b.X) &&
               AreFloatsEqual(a.Y, b.Y) &&
               AreFloatsEqual(a.Weight, b.Weight);
    }

    private static bool TrajectoryEqual(MqttTrajectory? a, MqttTrajectory? b)
    {
        return ReferenceEquals(a, b) ||
               (a is not null && b is not null &&
                AreIntsEqual(a.Degree, b.Degree) &&
                AreListsEqual(a.KnotVector, b.KnotVector, (x, y) => AreFloatsEqual(x, y)) &&
                AreListsEqual(a.ControlPoints, b.ControlPoints, ControlPointEqual));
    }

    private static bool EdgeStateEqual(MqttEdgeState a, MqttEdgeState b) =>
        EqualsIgnoreCase(a.EdgeId, b.EdgeId) &&
        EqualsIgnoreCase(a.EdgeDescription, b.EdgeDescription) &&
        a.Released == b.Released &&
        AreIntsEqual(a.SequenceId, b.SequenceId) &&
        TrajectoryEqual(a.Trajectory, b.Trajectory);

    private static bool ErrorReferenceEqual(MqttErrorReference a, MqttErrorReference b) =>
        EqualsIgnoreCase(a.ReferenceKey, b.ReferenceKey) &&
        EqualsIgnoreCase(a.ReferenceValue, b.ReferenceValue);

    private static bool ErrorEqual(MqttError a, MqttError b) =>
        EqualsIgnoreCase(a.ErrorDescription, b.ErrorDescription) &&
        EqualsIgnoreCase(a.ErrorType, b.ErrorType) &&
        a.ErrorLevel == b.ErrorLevel &&
        AreListsEqual(a.ErrorReferences, b.ErrorReferences, ErrorReferenceEqual);

    private static bool InfoReferenceEqual(MqttInfoReference a, MqttInfoReference b) =>
        EqualsIgnoreCase(a.ReferenceKey, b.ReferenceKey) &&
        EqualsIgnoreCase(a.ReferenceValue, b.ReferenceValue);

    private static bool InformationEqual(MqttInformation a, MqttInformation b) =>
        EqualsIgnoreCase(a.InfoDescription, b.InfoDescription) &&
        EqualsIgnoreCase(a.InfoType, b.InfoType) &&
        a.InfoLevel == b.InfoLevel &&
        AreListsEqual(a.InfoReferences, b.InfoReferences, InfoReferenceEqual);

    private static bool SafetyStateEqual(MqttSafetyState? a, MqttSafetyState? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        return a.EStop == b.EStop &&
               a.FieldViolation == b.FieldViolation;
    }
}