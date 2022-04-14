namespace Gebhardt.StoreWare.Agv.Common.Messages
{
    /// <summary>
    /// Dummy message
    /// </summary>
    public class DummyMessage
    {
        /// <summary>Id</summary>
        public int Id { get; init; }

        /// <summary>Timestamp</summary>
        private DateTime? Timestamp { get; } = DateTime.Now;

        /// <summary>Content</summary>
        public string? Content { get; init; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Id}|{Timestamp:yyyy-MM-dd H:mm:ss}|{Content}";
        }
    }
}