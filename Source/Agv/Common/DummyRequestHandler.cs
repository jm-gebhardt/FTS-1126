using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Handler for dummy messages
    /// </summary>
    public class DummyRequestHandler
    {
        private readonly IBus _bus;

        /// <summary>
        /// Create new DummyHandler
        /// </summary>
        /// <param name="bus">MassTransit Bus</param>
        public DummyRequestHandler(IBus bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// Handle Dummy message
        /// </summary>
        /// <param name="message"></param>
        public void HandleMessage(Messages.DummyRequest message)
        {
            Console.WriteLine($"Request received: {message}");

            Messages.DummyReply reply = new() { Id = message.Id, Content = "HostOrder Confirmation" };
            _bus.Publish(reply);

            Console.WriteLine($"Reply send: {reply}");
        }
    }
}
