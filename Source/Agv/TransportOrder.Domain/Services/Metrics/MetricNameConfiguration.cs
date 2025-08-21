namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides metric names.
/// </summary>
internal static class MetricNameConfiguration
{
    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.BatteryChargeGauge"/>.
    /// </summary>
    internal const string BatteryChargeMetric = "ACC_AGV_BATTERY_CHARGE";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvCompletedOrdersCounter"/>.
    /// </summary>
    internal const string AgvCompletedOrdersCountMetric = "ACC_AGV_COMPLETED_ORDERS_COUNT";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvTravelDistanceGauge"/>.
    /// </summary>
    internal const string AgvTravelDistance = "ACC_AGV_TRAVEL_DISTANCE";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvStateBusyGauge"/>.
    /// </summary>
    internal const string AgvStateBusy = "ACC_AGV_STATE_BUSY_DURATION";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvStateBusyCargoGauge"/>.
    /// </summary>
    internal const string AgvStateBusyCargo = "ACC_AGV_STATE_BUSY_CARGO_DURATION";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvStateChargingGauge"/>.
    /// </summary>
    internal const string AgvStateCharging = "ACC_AGV_STATE_CHARGING_DURATION";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvStateIdleGauge"/>.
    /// </summary>
    internal const string AgvStateIdle = "ACC_AGV_STATE_IDLE_DURATION";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.AgvSafetyFieldViolationCounter"/>.
    /// </summary>
    internal const string AgvSafetyFieldViolationCount = "ACC_AGV_SAFETYFIELD_VIOLATION_COUNT";

    /// <summary>
    /// Metric name for  <see cref="PrometheusMetricCollector.AgvSafetyFieldDurationGauge"/>.
    /// </summary>
    internal const string AgvSafetyFieldViolationDuration = "ACC_AGV_SAFETYFIELD_VIOLATION_DURATION";

    /// <summary>
    /// Metric name for <see cref="PrometheusMetricCollector.StationJamStateCounter"/>.
    /// </summary>
    internal const string StationJamStateCount = "ACC_STATION_JAMSTATE_COUNT";

    /// <summary>
    /// Metric name for  <see cref="PrometheusMetricCollector.StationJamStateDurationGauge"/>.
    /// </summary>
    internal const string StationJamStateDuration = "ACC_STATION_JAMSTATE_DURATION";
}