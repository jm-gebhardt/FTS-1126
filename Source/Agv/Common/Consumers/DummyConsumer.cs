using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.Common.Consumers
{
    /// <summary>
    /// Dummy Consumer
    /// </summary>
    public class DummyConsumer :
        IConsumer<DummyRequest>,
        IConsumer<DummyReply>
    {
        private readonly DummyRequestHandler? _requestHandler;
        private readonly DummyReplyHandler? _replyHandler;

        /// <summary>
        /// Create dummy consumer with message handler delegate
        /// </summary>
        /// <param name="handler">Handler delegate for processing messages</param>
        public DummyConsumer(DummyRequestHandler handler)
        {
            _requestHandler = handler;
        }

        /// <summary>
        /// Create dummy consumer with message handler delegate
        /// </summary>
        /// <param name="handler">Handler delegate for processing messages</param>
        public DummyConsumer(DummyReplyHandler handler)
        {
            _replyHandler = handler;
        }

        /// <summary>
        /// Consume DummyRequest
        /// </summary>
        public async Task Consume(ConsumeContext<DummyRequest> context)
        {
            await Task.Run(() => { _requestHandler?.HandleMessage(context.Message); });
        }

        /// <summary>
        /// Consume DummyReply
        /// </summary>
        public async Task Consume(ConsumeContext<DummyReply> context)
        {
            await Task.Run(() => { _replyHandler?.HandleMessage(context.Message); });
        }
    }
}