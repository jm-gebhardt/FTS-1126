using Prometheus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides metrics exposed at prometheus endpoint.
/// </summary>
public static class MetricCollector
{
    /// <summary>
    /// The battery charge gauge metric.
    /// </summary>
    public static readonly Gauge BatteryChargeGauge = Prometheus.Metrics.CreateGauge(MetricNameConfiguration.BatteryChargeMetric, "Battery charge level", new GaugeConfiguration
    {
        LabelNames = [MetricLabelConfiguration.Id]
    });
}