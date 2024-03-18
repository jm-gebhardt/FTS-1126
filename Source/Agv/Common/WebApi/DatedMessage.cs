namespace Gebhardt.StoreWare.Agv.Common.WebApi;

/// <inheritdoc />
public record DatedMessage : IMessageCreatedDate
{
    /// <inheritdoc />
    public DateTime MessageCreatedDate { get; init; } = DateTime.UtcNow;
}