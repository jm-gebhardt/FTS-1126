using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.BackgroundEditor;

/// <summary>
/// Represents the response to the <see cref="GetBackgrounds"/> request.
/// </summary>
/// <param name="Backgrounds">The backgrounds.</param>
public record GotBackgrounds(ICollection<Background> Backgrounds);