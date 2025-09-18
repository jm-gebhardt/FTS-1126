using Prometheus;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides metrics exposed at prometheus endpoint.
/// </summary>
public static class PrometheusMetricCollector
{
    /// <summary>
    /// The battery charge gauge metric.
    /// </summary>
    public static readonly Gauge BatteryChargeGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.BatteryChargeMetric, "Battery charge level", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The completed orders by agv counter.
    /// </summary>
    public static readonly Counter AgvCompletedOrdersCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvCompletedOrdersCountMetric, "Completed orders count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv travel distance gauge metric.
    /// </summary>
    public static readonly Gauge AgvTravelDistanceGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvTravelDistance, "Agv travel distance [m]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv busy (w/o cargo) state duration gauge metric.
    /// </summary>
    public static readonly Gauge AgvStateBusyGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvStateBusy, "Agv busy state (w/o cargo) duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv busy (with cargo) state duration gauge metric.
    /// </summary>
    public static readonly Gauge AgvStateBusyCargoGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvStateBusyCargo, "Agv busy state (with cargo) duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv busy (with cargo) state duration gauge metric.
    /// </summary>
    public static readonly Gauge AgvStateChargingGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvStateCharging, "Agv charging state duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv idle state duration gauge metric.
    /// </summary>
    public static readonly Gauge AgvStateIdleGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvStateIdle, "Agv idle state duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The safety field violation by agv counter.
    /// </summary>
    public static readonly Counter AgvSafetyFieldViolationCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvSafetyFieldViolationCount, "Safety field violation count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The agv safety field duration gauge metric.
    /// </summary>
    public static readonly Gauge AgvSafetyFieldDurationGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvSafetyFieldViolationDuration, "Agv safety field violation duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The jam state by station counter.
    /// </summary>
    public static readonly Counter StationJamStateCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.StationJamStateCount, "Station jam state count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The station jam state duration gauge metric.
    /// </summary>
    public static readonly Gauge StationJamStateDurationGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.StationJamStateDuration, "Station jam state duration [s]", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The completed pick per station counter.
    /// </summary>
    public static readonly Counter StationPickCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.StationPickCount, "Station pick count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The completed drop per station counter.
    /// </summary>
    public static readonly Counter StationDropCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.StationDropCount, "Station drop count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The completed charge per station counter.
    /// </summary>
    public static readonly Counter StationChargeCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.StationChargeCount, "Station charge count", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });
}