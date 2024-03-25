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
    public static readonly Gauge AgvTravelDistanceGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.AgvTravelDistance, "Agv travel distance", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });
}