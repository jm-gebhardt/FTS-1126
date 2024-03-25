using Prometheus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

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
}