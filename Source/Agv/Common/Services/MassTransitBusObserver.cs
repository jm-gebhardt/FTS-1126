using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.Common.Services
{
    /// <summary>
    /// Observing MassTransit bus events
    /// </summary>
    public class BusObserver : IBusObserver
    {
        /// <inheritdoc />
        public void CreateFaulted(Exception exception)
        {
        }

        /// <inheritdoc />
        public void PostCreate(IBus bus)
        {
        }

        /// <inheritdoc />
        public Task PostStart(IBus bus, Task<BusReady> busReady)
        {
            return bus.Publish(new MassTransitBusEvent(MassTransitBusEvent.EventType.PostStarted));
        }

        /// <inheritdoc />
        public Task PostStop(IBus bus)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PreStart(IBus bus)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PreStop(IBus bus)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StartFaulted(IBus bus, Exception exception)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopFaulted(IBus bus, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}
