namespace Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;

/// <summary>
/// Internal message used to trigger periodic, cyclic tasks within the <see cref="TrafficManagementEventHandler"/>.
/// Published unconditionally on a fixed interval by <see cref="TrafficManagementTickPublisher"/>; the consumer
/// itself is responsible for discarding ticks that arrive too closely after the previous one was processed
/// (e.g. due to backlog), see <see cref="TrafficManagementTickState"/>.
/// </summary>
internal sealed record TrafficManagementTick;
