namespace Gebhardt.StoreWare.Agv.Common.Messages;

/// <inheritdoc />
public record DatedMessage : IMessageCreatedDate
{
    /// <inheritdoc />
    public DateTime MessageCreatedDate { get; init; } = DateTime.UtcNow;
}