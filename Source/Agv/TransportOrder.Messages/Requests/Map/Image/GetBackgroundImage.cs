namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

/// <summary>Query to retrieve the PNG bytes of the background image.</summary>
/// <param name="CorrelationId">The correlation id of the map.</param>
public record GetBackgroundImage(Guid CorrelationId);