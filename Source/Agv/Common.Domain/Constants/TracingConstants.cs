namespace Gebhardt.Acc.Common.Domain.Constants;

/// <summary>
/// Constants for distributed tracing operations, tags, and event names.
/// </summary>
public static class TracingConstants
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    // Tag Names - ActionGroup
    public const string TagActionGroupCorrelationId = "actiongroup.correlation_id";
    public const string TagActionGroupOrderId = "actiongroup.order_id";
    public const string TagActionGroupHu = "actiongroup.hu";

    // Tag Names - AGV
    public const string TagAgvCorrelationId = "agv.correlation_id";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}