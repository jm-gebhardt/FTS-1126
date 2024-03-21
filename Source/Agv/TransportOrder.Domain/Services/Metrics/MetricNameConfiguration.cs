namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

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
}