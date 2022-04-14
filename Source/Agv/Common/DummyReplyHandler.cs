namespace Gebhardt.StoreWare.Agv.Common
{
    /// <summary>
    /// Handler for dummy messages
    /// </summary>
    public class DummyReplyHandler
    {
        /// <summary>
        /// Handle Dummy message
        /// </summary>
        /// <param name="message"></param>
        public void HandleMessage(Messages.DummyReply message)
        {
            Console.WriteLine($"Reply received: {message}");
        }
    }
}
