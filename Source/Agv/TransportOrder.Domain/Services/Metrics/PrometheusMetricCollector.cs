using System.Collections.Concurrent;
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
    /// The accumulated agv travel distance counter metric.
    /// </summary>
    public static readonly Counter AgvTravelDistanceCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvTravelDistance, "Accumulated agv travel distance in meters", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The accumulated agv busy (w/o cargo) state duration counter metric.
    /// </summary>
    public static readonly Counter AgvStateBusyCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvStateBusy, "Accumulated agv busy state (w/o cargo) duration in seconds", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The accumulated agv busy (with cargo) state duration counter metric.
    /// </summary>
    public static readonly Counter AgvStateBusyCargoCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvStateBusyCargo, "Accumulated agv busy state (with cargo) duration in seconds", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The accumulated agv charging state duration counter metric.
    /// </summary>
    public static readonly Counter AgvStateChargingCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvStateCharging, "Accumulated agv charging state duration in seconds", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The accumulated agv idle state duration counter metric.
    /// </summary>
    public static readonly Counter AgvStateIdleCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvStateIdle, "Accumulated agv idle state duration in seconds", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// The accumulated agv offline state duration counter metric.
    /// </summary>
    public static readonly Counter AgvStateOfflineCounter = Prometheus.Metrics.CreateCounter(MetricNameConfiguration.AgvStateOffline, "Accumulated agv offline state duration in seconds", new CounterConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });

    /// <summary>
    /// Tracks when each AGV went offline for duration calculation.
    /// </summary>
    public static readonly ConcurrentDictionary<Guid, DateTime> AgvOfflineSince = new();

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