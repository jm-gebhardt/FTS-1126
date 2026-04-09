using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Represents the response to the <see cref="GetBackground"/> request.
/// </summary>
/// <param name="Background">The background.</param>
public record GotBackground(Background Background);