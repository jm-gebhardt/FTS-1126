using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Response to the <see cref="AddOrUpdateBackground"/> request.
/// </summary>
/// <param name="Background">The persisted background domain object.</param>
public record BackgroundAddedOrUpdated(Background Background);
