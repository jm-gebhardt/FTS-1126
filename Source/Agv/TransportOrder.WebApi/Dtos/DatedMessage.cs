namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

/// <inheritdoc />
public record DatedMessage : IMessageCreatedDate
{
    /// <inheritdoc />
    public DateTime MessageCreatedDate { get; init; } = DateTime.UtcNow;
}